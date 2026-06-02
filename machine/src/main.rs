mod config;
mod simulation;
mod state;
pub mod comms {
    tonic::include_proto!("comms");
}

use std::{
    sync::{Arc, Mutex},
    time::Duration,
};

use comms::machine_telemetry_client::MachineTelemetryClient;
use tokio::sync::mpsc;
use tokio_stream::wrappers::ReceiverStream;
use tonic::Request;
use tracing::{debug, error, info, warn};

use crate::comms::TelemetryMessage;

#[tokio::main]
async fn main() {
    setup_pretty_logging();

    let cfg = config::Config::from_env();
    info!(target: "boot", "🏭 machine controller starting: {:?}", cfg);

    let state = Arc::new(Mutex::new(state::State::new(&cfg)));

    let mut backoff_secs = 1u64;
    loop {
        match run(&cfg, state.clone()).await {
            Ok(()) => {
                // if some non-error scenario, reset backoff and try again
                warn!(target: "net", "stream closed for some reason; reconnecting…");
                backoff_secs = 1;
            }
            Err(err) => {
                error!(target: "net", "session error: {err}; retrying in {backoff_secs}s");
                tokio::time::sleep(Duration::from_secs(backoff_secs)).await;
                backoff_secs = (backoff_secs * 2).min(30);
            }
        }
    }
}

fn setup_pretty_logging() {
    tracing_subscriber::fmt()
        .with_ansi(true)
        .with_target(true)
        .with_env_filter(
            tracing_subscriber::EnvFilter::try_from_default_env()
                .unwrap_or_else(|_| tracing_subscriber::EnvFilter::new("info,telemetry=debug")),
        )
        .init();
}

async fn run(cfg: &config::Config, state: Arc<Mutex<state::State>>) -> Result<(), tonic::Status> {
    let mut client = MachineTelemetryClient::connect(cfg.backend_url.clone())
        .await
        .map_err(|err| tonic::Status::unavailable(format!("connection failed: {err}")))?;

    info!(target: "net", "connected to backend at {}", cfg.backend_url);

    let (tx, rx) = mpsc::channel::<TelemetryMessage>(128);
    let outbound = ReceiverStream::new(rx);

    let telemetry_task = {
        let cfg = cfg.clone();
        let state = state.clone();
        tokio::spawn(async move {
            loop {
                let sleep_ms = {
                    let s = state.lock().unwrap();
                    ((cfg.telemetry_interval_ms as f32) * s.sim_speed).max(100.0) as u64
                };

                tokio::time::sleep(Duration::from_millis(sleep_ms)).await;

                let msg = {
                    let mut s = state.lock().unwrap();
                    s.simulation_tick();
                    s.build_telemetry_message(&cfg)
                };

                debug!(
                    target: "telemetry",
                    "{} part={} {}°C vib={} load={}% q={} [{}/{}]",
                    msg.machine_id, msg.part_id, msg.temperature, msg.vibration,
                    msg.spindle_load, msg.quality_score, msg.status, msg.current_part_status
                );

                if tx.send(msg).await.is_err() {
                    break;
                }
            }
        })
    };

    let part_task = {
        let cfg = cfg.clone();
        let state = state.clone();
        tokio::spawn(async move {
            loop {
                let sleep_ms = {
                    let s = state.lock().unwrap();
                    ((cfg.part_cycle_base_ms as f32) * s.sim_speed).max(100.0) as u64
                };

                tokio::time::sleep(Duration::from_millis(sleep_ms)).await;

                let mut s = state.lock().unwrap();
                s.advance_part(&cfg);
            }
        })
    };

    // capture this thread's communications task in this result
    let result: Result<(), tonic::Status> = async {
        let response = client.telemetry_stream(Request::new(outbound)).await?;
        let mut inbound = response.into_inner();
        info!(target: "net", "telemetry stream open; awaiting commands");
        while let Some(cmd) = inbound.message().await? {
            info!(target: "cmd", "↩ received {} (id={})", cmd.command_type, cmd.command_id);
            // TODO
        }

        Ok(())
    }
    .await;

    // so that we can kill off the child threads if the comms fail
    telemetry_task.abort();
    part_task.abort();

    // return whatever the result was
    result
}
