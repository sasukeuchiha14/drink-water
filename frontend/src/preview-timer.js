export class PreviewTimer {
  constructor(clock = () => performance.now()) {
    this.clock = clock;
    this.minutes = 30;
    this.enabled = true;
    this.deadline = this.clock() + this.minutes * 60_000;
  }

  setInterval(minutes) {
    if (![10, 30, 60].includes(minutes)) {
      throw new RangeError("Choose 10, 30, or 60 minutes.");
    }
    if (minutes !== this.minutes) {
      this.minutes = minutes;
      this.restart();
    }
  }

  setEnabled(enabled) {
    if (enabled && !this.enabled) {
      this.restart();
    }
    this.enabled = enabled;
  }

  restart() {
    this.deadline = this.clock() + this.minutes * 60_000;
  }

  remainingSeconds() {
    if (!this.enabled) {
      return null;
    }
    return Math.max(0, Math.ceil((this.deadline - this.clock()) / 1000));
  }

  tick() {
    if (this.remainingSeconds() !== 0) {
      return false;
    }
    this.restart();
    return true;
  }
}

export function formatCountdown(seconds) {
  if (seconds === null) {
    return "Paused";
  }
  const rounded = Math.max(0, Math.ceil(seconds));
  const hours = Math.floor(rounded / 3600);
  const minutes = Math.floor((rounded % 3600) / 60);
  const remainder = rounded % 60;
  const twoDigits = (value) => String(value).padStart(2, "0");
  return hours > 0
    ? hours + ":" + twoDigits(minutes) + ":" + twoDigits(remainder)
    : twoDigits(minutes) + ":" + twoDigits(remainder);
}
