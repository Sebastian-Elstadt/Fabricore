use rand::{RngExt, rngs::ThreadRng};
use std::time::{Duration, Instant};
use tracing::{warn, info};

use crate::{
    config::Config,
    state::{PartPhase, RunState, State},
};

impl State {
    pub fn advance_part(&mut self, cfg: &Config) {
        if self.run_state != RunState::Running {
            return;
        }

        match self.part_phase {
            PartPhase::Idle => {
                if self.current_part_id.is_none() {
                    if !cfg.is_source_machine() {
                        return;
                    }

                    self.part_counter += 1;
                    let part_id = format!("PART-{:04}", 1000 + self.part_counter);
                    info!(target: "part", "🟢 new part spawned: {part_id}");
                    self.current_part_id = Some(part_id);
                }

                self.part_phase = PartPhase::InProgress;
                self.part_started = Some(Instant::now());
                self.cycle_time_sec = 0.0;
            }
            PartPhase::InProgress => {
                self.part_phase = PartPhase::Completed;
                let part_id = self.current_part_id.clone().unwrap_or_default();
                info!(target: "part", "✅ part {part_id} completed (q={:.0})", self.quality_score);
            }
            PartPhase::Completed => {
                self.current_part_id = None;
                self.part_phase = PartPhase::Idle;
                self.cycle_time_sec = 0.0;
                self.part_quarantined = false;
            }
        }
    }

    pub fn simulation_tick(&mut self) {
        // measure time since last tick
        let now = Instant::now();
        let dt = (now - self.last_tick).as_secs_f32().clamp(0.05, 5.0);
        self.last_tick = now;

        // random events
        let mut rng = rand::rng();
        self.apply_random_events(now, &mut rng);

        // based on the situation, simulate normal operation toward some target state
        let (temp, vib, load, rate) = self.get_target_state(now);

        let approach = 1.0_f32 - (1.0_f32 - rate).powf(dt); // step just a little bit toward the target state
        self.temperature += (temp - self.temperature) * approach + rng.random_range(-0.4..0.4);
        self.vibration += (vib - self.vibration) * approach + rng.random_range(-0.15..0.15);
        self.spindle_load += (load - self.spindle_load) * approach + rng.random_range(-1.0..1.0);

        if self.has_defect_boost(now) {
            // spike it
            self.temperature += 2.0;
            self.vibration += 3.0;
        }

        // restrictions
        self.temperature = self.temperature.clamp(18.0, 125.0);
        self.vibration = self.vibration.max(0.0);
        self.spindle_load = self.spindle_load.clamp(0.0, 100.0);

        // track quality
        self.track_quality_score(approach, &mut rng);

        // track time spent on part
        self.track_cycle_time(now);

        // cleanup
        self.clear_expired_events(now);
    }

    fn track_cycle_time(&mut self, now: Instant) {
        if let (PartPhase::InProgress, Some(started)) = (self.part_phase, self.part_started) {
            self.cycle_time_sec = (now - started).as_secs_f32();
        }
    }

    fn track_quality_score(&mut self, approach: f32, rng: &mut ThreadRng) {
        // derive some form of a quality score at this point in time
        let mut q_target = 97.0;
        if self.temperature > 85.0 {
            q_target -= (self.temperature - 85.0) * 1.5; // scaling penalty
        }

        if self.vibration > 6.0 {
            q_target -= (self.vibration - 6.0) * 2.0; // scaling penalty
        }

        // nudge overall quality score
        self.quality_score +=
            (q_target - self.quality_score) * (approach * 0.5) + rng.random_range(-0.3..0.3);
        self.quality_score = self.quality_score.clamp(0.0, 100.0);
    }

    // temp, vibrations, spindle load, rate
    fn get_target_state(&self, now: Instant) -> (f32, f32, f32, f32) {
        if self.run_state == RunState::Stopped {
            (24.0, 0.3, 0.0, 0.30) // dead
        } else if self.is_cooling(now) {
            (35.0, 1.5, self.load_target * 0.3, 0.30) // slowed down
        } else if self.is_overheating(now) {
            (96.0, 7.5, self.load_target, 0.25) // going crazy
        } else if self.run_state == RunState::Paused {
            (38.0, 0.8, 0.0, 0.12) // halfway between busy and dead
        } else if self.part_phase == PartPhase::InProgress {
            (
                58.0 + self.load_target * 0.22,
                3.0 + self.load_target * 0.03,
                self.load_target,
                0.15,
            ) // busy
        } else {
            (42.0, 1.0, 6.0, 0.10) // running but idle
        }
    }

    fn apply_random_events(&mut self, now: Instant, rng: &mut ThreadRng) {
        if self.run_state == RunState::Running
            && self.part_phase == PartPhase::InProgress
            && !self.is_overheating(now)
            && !self.is_cooling(now)
            && rng.random_bool(0.04)
        {
            let secs = rng.random_range(8.0..16.0);
            self.overheat_until = Some(now + Duration::from_secs_f32(secs));
            warn!(target: "sim", "🔥 spontaneous OVERHEAT event for {:.0}s", secs);
        }
    }

    fn clear_expired_events(&mut self, now: Instant) {
        if self.overheat_until.map(|t| now >= t).unwrap_or(false) {
            self.overheat_until = None;
        }
        if self.cooldown_until.map(|t| now >= t).unwrap_or(false) {
            self.cooldown_until = None;
        }
        if self.defect_boost_until.map(|t| now >= t).unwrap_or(false) {
            self.defect_boost_until = None;
        }
    }
}
