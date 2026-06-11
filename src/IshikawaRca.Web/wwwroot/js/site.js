document.addEventListener("DOMContentLoaded", () => {
  const filterBar = document.querySelector("[data-timeline-filter-bar]");
  const timelineItems = document.querySelectorAll("[data-timeline-kind]");

  if (!filterBar || timelineItems.length === 0) {
    return;
  }

  filterBar.addEventListener("click", (event) => {
    const button = event.target.closest("[data-timeline-filter]");

    if (!button) {
      return;
    }

    const filter = button.dataset.timelineFilter;

    filterBar.querySelectorAll("[data-timeline-filter]").forEach((item) => {
      item.classList.toggle("is-active", item === button);
    });

    timelineItems.forEach((item) => {
      const shouldShow = filter === "all" || item.dataset.timelineKind === filter;
      item.classList.toggle("is-hidden", !shouldShow);
    });
  });
});
