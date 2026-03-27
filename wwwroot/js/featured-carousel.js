document.addEventListener('DOMContentLoaded', function () {
    const container = document.querySelector('.featured-carousel');
    if (!container) return;
    const track = container.querySelector('.featured-carousel-track');
    const items = container.querySelectorAll('.project-card');
    const prevBtn = container.querySelector('.carousel-arrow--left');
    const nextBtn = container.querySelector('.carousel-arrow--right');
    let index = 0;
    const visibleCount = 5;
    const maxIndex = Math.max(0, items.length - visibleCount);

    function update() {
        const offset = index * (items[0].offsetWidth + 16); // 16px gap
        track.style.transform = `translateX(-${offset}px)`;
        prevBtn.disabled = index === 0;
        nextBtn.disabled = index === maxIndex;
    }

    prevBtn.addEventListener('click', function () {
        if (index > 0) {
            index--;
            update();
        }
    });
    nextBtn.addEventListener('click', function () {
        if (index < maxIndex) {
            index++;
            update();
        }
    });
    window.addEventListener('resize', update);
    update();
});
