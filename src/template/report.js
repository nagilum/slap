"use strict";

/**
 * Toggle collapsed state of the indicated info panel.
 * @param {Event} e Click event.
 */
const toggleInfoPanel = (e) => {
    const id = e.target.getAttribute('data-id');

    document
        .querySelectorAll('div.info-panel')
        .forEach(d => {
            const did = d.getAttribute('id');

            if (did === id) {
                d.classList.remove('hidden');
            }
            else {
                d.classList.add('hidden');
            }
        });
};

/**
 * Toggle the element inner text with an attribute value.
 * @param {Event} e Click event.
 */
const toggleText = (e) => {
    const text = e.target.getAttribute('data-text');
    const value = e.target.innerText;

    e.target.setAttribute('data-text', value);
    e.target.innerText = text;
};

/**
 * Init all the things..
 */
(() => {
    // Toggle collapsed state of the indicated info panel.
    document
        .querySelectorAll('a.toggle-info-panel')
        .forEach(a => {
            a.addEventListener('click', toggleInfoPanel);
        });

    // Toggle the element inner text with an attribute value.
    document
        .querySelectorAll('a.toggle-text')
        .forEach(a => {
            a.addEventListener('click', toggleText);
        });
})();