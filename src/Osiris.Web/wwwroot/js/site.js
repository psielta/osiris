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

// Submit-loading: a submit button with a `data-busy` attribute shows a spinner + busy label and
// disables itself (and sibling submit buttons) once its form is submitted, preventing double-submit
// on slow actions (e.g. the AI PDF import). State is reset on back/forward (bfcache) navigation.
(function () {
    function showBusy(button) {
        if (button.dataset.originalHtml === undefined) {
            button.dataset.originalHtml = button.innerHTML;
        }
        button.innerHTML = '<span class="js-busy-spinner" aria-hidden="true"></span>' + button.getAttribute('data-busy');
        button.disabled = true;
        const form = button.form;
        if (form) {
            form.querySelectorAll('button[type="submit"]').forEach((other) => {
                if (other !== button) {
                    other.disabled = true;
                }
            });
        }
    }

    function restore(button) {
        button.disabled = false;
        if (button.dataset.originalHtml !== undefined) {
            button.innerHTML = button.dataset.originalHtml;
            delete button.dataset.originalHtml;
        }
    }

    document.addEventListener('submit', (event) => {
        const button = event.submitter;
        if (!button || !button.hasAttribute('data-busy')) {
            return;
        }
        // Defer so the native form submission starts before the button is disabled.
        setTimeout(() => showBusy(button), 0);
    });

    window.addEventListener('pageshow', () => {
        document.querySelectorAll('button[data-busy]').forEach(restore);
    });
})();
