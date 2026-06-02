use chrono::Utc;
use rand::RngExt;
use std::time::{Duration, Instant};
use tracing::{debug, error, info, warn};

use crate::{
    comms::{CommandMessage, TelemetryMessage},
    config::Config,
};

#[derive(Clone, Copy, PartialEq, Eq, Debug)]
pub enum RunState {
    Running,
    Paused,
    Stopped,
}

#[derive(Clone, Copy, PartialEq, Eq, Debug)]
pub enum PartPhase {
    Idle,
    InProgress,
    Completed,
}

pub struct State {
    pub run_state: RunState,

    // live sensor values
    pub temperature: f32,
    pub vibration: f32,
    pub spindle_load: f32,
    pub quality_score: f32,

    // target the live values drift toward
    pub load_target: f32,

    // part lifecycle
    pub part_phase: PartPhase,
    pub current_part_id: Option<String>,
    pub part_quarantined: bool,
    pub part_started: Option<Instant>,
    pub cycle_time_sec: f32,
    pub part_counter: u32,

    // events (active until)
    pub overheat_until: Option<Instant>,
    pub cooldown_until: Option<Instant>,
    pub defect_boost_until: Option<Instant>,

    // simulation
    pub sim_speed: f32,
    pub last_tick: Instant,

    pub last_command: Option<String>,
}

impl State {
    pub fn new(cfg: &Config) -> Self {
        State {
            run_state: RunState::Running,
            temperature: 45.0,
            vibration: 2.5,
            spindle_load: 40.0,
            quality_score: 98.0,
            load_target: 55.0,
            part_phase: PartPhase::Idle,
            current_part_id: None,
            part_quarantined: false,
            part_started: None,
            cycle_time_sec: 0.0,
            part_counter: 0,
            overheat_until: None,
            cooldown_until: None,
            defect_boost_until: None,
            sim_speed: cfg.sim_speed,
            last_tick: Instant::now(),
            last_command: None,
        }
    }

    pub fn is_overheating(&self, now: Instant) -> bool {
        self.overheat_until.map(|t| now < t).unwrap_or(false)
    }

    pub fn is_cooling(&self, now: Instant) -> bool {
        self.cooldown_until.map(|t| now < t).unwrap_or(false)
    }

    pub fn has_defect_boost(&self, now: Instant) -> bool {
        self.defect_boost_until.map(|t| now < t).unwrap_or(false)
    }

    pub fn status_str(&self, now: Instant) -> &'static str {
        match self.run_state {
            RunState::Stopped => "stopped",
            RunState::Paused => "paused",
            RunState::Running => {
                if self.is_overheating(now) {
                    "fault"
                } else if self.part_phase == PartPhase::InProgress {
                    "processing"
                } else {
                    "idle"
                }
            }
        }
    }

    pub fn part_status_str(&self) -> &'static str {
        match self.part_phase {
            PartPhase::Idle => "",
            PartPhase::InProgress => "in_progress",
            PartPhase::Completed => {
                if self.part_quarantined {
                    "quarantined"
                } else {
                    "completed"
                }
            }
        }
    }

    pub fn build_telemetry_message(&self, cfg: &Config) -> TelemetryMessage {
        let now = Instant::now();
        TelemetryMessage {
            machine_id: cfg.machine_id.clone(),
            part_id: self.current_part_id.clone().unwrap_or_default(),
            timestamp_ms: Utc::now().timestamp_millis(),
            temperature: round(self.temperature, 1),
            vibration: round(self.vibration, 1),
            spindle_load: round(self.spindle_load, 1),
            cycle_time_sec: round(self.cycle_time_sec, 1),
            quality_score: round(self.quality_score, 1),
            status: self.status_str(now).to_string(),
            current_part_status: self.part_status_str().to_string(),
        }
    }
}

fn round(num: f32, decimal_places: u8) -> f32 {
    let x = (decimal_places as f32) * 10.0;
    (num * x).round() / x
}
