function setTheme(themeName) {
    document.documentElement.setAttribute('data-theme', themeName);
    document.cookie = `idp_theme=${themeName}; path=/; max-age=31536000; SameSite=Lax`;
    
    document.querySelectorAll('.theme-swatch').forEach(el => {
        el.classList.remove('active');
        if(el.dataset.theme === themeName) {
            el.classList.add('active');
        }
    });
}

document.addEventListener("DOMContentLoaded", () => {
    let match = document.cookie.match(/(?:^|; )idp_theme=([^;]*)/);
    let currentTheme = match ? match[1] : 'slate';
    setTheme(currentTheme);
});
