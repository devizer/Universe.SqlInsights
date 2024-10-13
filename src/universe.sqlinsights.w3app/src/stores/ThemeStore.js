const mediaQuery = '(prefers-color-scheme: dark)'

const listeners = [];

const on = listener => {
    listeners.push(listener);
}

const off = listener => {
    const index = listeners.indexOf(listener);
    if (index !== -1) {
        listeners.splice(index, 1);
    }
}

const notifyAll = systemTheme => {
    listeners.forEach(listener => {
        if (listener) listener(systemTheme);
    });
}

// Get's current system theme dark|light
function getSystemTheme() {
    if (global && global.matchMedia) {
        if (global.matchMedia(mediaQuery).matches) {
            return 'dark'
        }
    }

    return 'light'
}

if (global && global.matchMedia && global.matchMedia(mediaQuery) && global.matchMedia(mediaQuery).addEventListener) {
    console.log(`%c SUBSCRIBING TO THEME CHANGED EVENT. Initial Theme: ${getSystemTheme()}`, "color: DarkRed");
    global.matchMedia(mediaQuery).addEventListener('change', (event) => {
        const prefersDarkMode = event.matches;
        const themeName = prefersDarkMode ? 'dark' : 'light'; 
        
        notifyAll(themeName);
    })
}

const ThemeStore = {
    on,
    off,
    getSystemTheme
}

export default ThemeStore;