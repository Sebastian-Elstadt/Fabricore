use std::env;

#[derive(Clone, Debug)]
pub struct Config {
    pub machine_id: String,
    pub backend_url: String,
    pub sim_speed: f32,
    pub part_cycle_base_ms: u64,
    pub telemetry_interval_ms: u64,
}

impl Config {
    pub fn from_env() -> Self {
        Config {
            machine_id: env::var("MACHINE_ID").unwrap_or_else(|_| "M1".to_string()),
            backend_url: env::var("BACKEND_GRPC_URL")
                .unwrap_or_else(|_| "http://127.0.0.1:50051".to_string()),
            sim_speed: var_or("SIM_SPEED", 1.0_f32),
            part_cycle_base_ms: var_or("PART_CYCLE_BASE_MS", 8000_u64),
            telemetry_interval_ms: var_or("TELEMETRY_INTERVAL_MS", 3000_u64),
        }
    }

    pub fn is_source_machine(&self) -> bool {
        self.machine_id.eq_ignore_ascii_case("M1")
    }
}

fn var_or<T: std::str::FromStr>(key: &str, default: T) -> T {
    env::var(key)
        .ok()
        .and_then(|v| v.parse().ok())
        .unwrap_or(default)
}