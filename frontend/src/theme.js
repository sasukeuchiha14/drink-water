(() => {
  const storageKey = "drink-water-theme";
  const systemTheme = window.matchMedia("(prefers-color-scheme: dark)");
  let preference;
  const labels =
    document.documentElement.lang === "hi"
      ? { dark: "गहरे रंग की थीम चुनें", light: "हल्के रंग की थीम चुनें" }
      : { dark: "Switch to dark theme", light: "Switch to light theme" };
  try {
    const saved = localStorage.getItem(storageKey);
    if (saved === "light" || saved === "dark") preference = saved;
  } catch {
    // The site still works when browser storage is blocked.
  }

  function applyTheme() {
    const dark = preference ? preference === "dark" : systemTheme.matches;
    document.documentElement.dataset.theme = dark ? "dark" : "light";
    const button = document.querySelector("#theme-toggle");
    if (button) {
      button.setAttribute("aria-pressed", String(dark));
      button.setAttribute("aria-label", dark ? labels.light : labels.dark);
      button.title = dark ? labels.light : labels.dark;
    }
    document
      .querySelector('meta[name="theme-color"]')
      ?.setAttribute("content", dark ? "#111b25" : "#f8fafb");
  }

  applyTheme();
  systemTheme.addEventListener("change", () => {
    if (!preference) applyTheme();
  });
  document.addEventListener("DOMContentLoaded", () => {
    applyTheme();
    document.querySelector("#theme-toggle")?.addEventListener("click", () => {
      preference =
        document.documentElement.dataset.theme === "dark" ? "light" : "dark";
      try {
        localStorage.setItem(storageKey, preference);
      } catch {
        // A session-only theme remains available without storage.
      }
      applyTheme();
    });
  });
})();
