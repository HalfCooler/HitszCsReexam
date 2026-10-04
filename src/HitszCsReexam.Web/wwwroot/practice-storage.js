window.practiceStorage = {
    load(key) { return localStorage.getItem(key); },
    save(key, value) { localStorage.setItem(key, value); }
};
