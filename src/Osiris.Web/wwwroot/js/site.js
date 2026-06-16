// App-level browser behavior.

// Theme store: light / dark / system, persisted in localStorage.
// The no-flash bootstrap (_ThemeBootstrap.cshtml) already applied the correct class
// before paint; this store drives runtime toggling and the reactive toggle button.
document.addEventListener('alpine:init', () => {
    const STORAGE_KEY = 'theme';
    const media = window.matchMedia('(prefers-color-scheme: dark)');

    Alpine.store('theme', {
        // "light" | "dark" | "system"
        mode: localStorage.getItem(STORAGE_KEY) || 'system',

        init() {
            this.apply();
            // Keep up with the OS when following the system preference.
            media.addEventListener('change', () => {
                if (this.mode === 'system') {
                    this.apply();
                }
            });
        },

        // Whether dark is currently effective (used by the toggle for its icon/label).
        get isDark() {
            return this.mode === 'dark' || (this.mode === 'system' && media.matches);
        },

        apply() {
            document.documentElement.classList.toggle('dark', this.isDark);
        },

        set(mode) {
            this.mode = mode;
            try {
                localStorage.setItem(STORAGE_KEY, mode);
            } catch (e) { /* storage unavailable: keep in-memory only */ }
            this.apply();
        },

        // light -> dark -> system -> light
        cycle() {
            const next = { light: 'dark', dark: 'system', system: 'light' };
            this.set(next[this.mode] || 'light');
        },
    });
});
