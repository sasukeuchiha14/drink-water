export function parseSlogans(text) {
  const messages = [
    ...new Set(
      text
        .split(/\r?\n/)
        .map((line) => line.trim())
        .filter((line) => line && !line.startsWith("#")),
    ),
  ];
  if (
    !messages.length ||
    messages.length > 100 ||
    messages.some(
      (message) =>
        message.length > 200 || /[\u0000-\u001f\u007f]/.test(message),
    )
  ) {
    throw new RangeError(
      "Use 1 to 100 slogans, with at most 200 characters per line.",
    );
  }
  return messages;
}

export class SloganPreview {
  constructor(random = Math.random) {
    this.random = random;
    this.previous = null;
  }

  next(messages, randomize) {
    if (!messages.length) throw new RangeError("Add at least one slogan.");
    const choices =
      randomize && messages.length > 1
        ? messages.filter((message) => message !== this.previous)
        : [];
    this.previous = choices.length
      ? choices[Math.floor(this.random() * choices.length)]
      : messages[0];
    return this.previous;
  }
}
