document.addEventListener("DOMContentLoaded", () => {
  const filterBar = document.querySelector("[data-timeline-filter-bar]");
  const timelineItems = document.querySelectorAll("[data-timeline-kind]");

  if (filterBar && timelineItems.length > 0) {
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
  }

  const offlineBanner = document.querySelector("[data-offline-banner]");

  if (offlineBanner) {
    const syncOnlineState = () => {
      offlineBanner.hidden = navigator.onLine;
    };

    window.addEventListener("online", syncOnlineState);
    window.addEventListener("offline", syncOnlineState);
    syncOnlineState();
  }

  document.querySelectorAll("form").forEach((form) => {
    form.addEventListener("submit", () => {
      form.classList.add("is-submitting");
    });
  });

  const fishboneBoard = document.querySelector("[data-fishbone-board]");
  const fishboneViewport = document.querySelector("[data-fishbone-viewport]");

  if (!fishboneBoard || !fishboneViewport) {
    return;
  }

  let zoom = 1;
  let isPanning = false;
  let panStartX = 0;
  let panStartY = 0;
  let scrollStartLeft = 0;
  let scrollStartTop = 0;

  const applyZoom = () => {
    fishboneViewport.style.transform = `scale(${zoom})`;
  };

  document.querySelectorAll("[data-fishbone-zoom]").forEach((button) => {
    button.addEventListener("click", () => {
      const action = button.dataset.fishboneZoom;

      if (action === "fit") {
        zoom = 1;
        fishboneBoard.scrollLeft = 0;
        fishboneBoard.scrollTop = 0;
      } else if (action === "in") {
        zoom = Math.min(1.4, zoom + 0.1);
      } else {
        zoom = Math.max(0.75, zoom - 0.1);
      }

      applyZoom();
    });
  });

  fishboneViewport.addEventListener("pointerdown", (event) => {
    if (event.target.closest("button, a, input, select, textarea")) {
      return;
    }

    isPanning = true;
    panStartX = event.clientX;
    panStartY = event.clientY;
    scrollStartLeft = fishboneBoard.scrollLeft;
    scrollStartTop = fishboneBoard.scrollTop;
    fishboneBoard.classList.add("is-panning");
    fishboneViewport.setPointerCapture(event.pointerId);
  });

  fishboneViewport.addEventListener("pointermove", (event) => {
    if (!isPanning) {
      return;
    }

    fishboneBoard.scrollLeft = scrollStartLeft - (event.clientX - panStartX);
    fishboneBoard.scrollTop = scrollStartTop - (event.clientY - panStartY);
  });

  const stopPanning = (event) => {
    if (!isPanning) {
      return;
    }

    isPanning = false;
    fishboneBoard.classList.remove("is-panning");

    if (fishboneViewport.hasPointerCapture(event.pointerId)) {
      fishboneViewport.releasePointerCapture(event.pointerId);
    }
  };

  fishboneViewport.addEventListener("pointerup", stopPanning);
  fishboneViewport.addEventListener("pointercancel", stopPanning);

  const detailPanel = document.querySelector("[data-detail-panel]");
  const detailTitle = document.querySelector("[data-detail-title]");
  const detailKicker = document.querySelector("[data-detail-kicker]");
  const detailBody = document.querySelector("[data-detail-body]");
  const detailMeta = document.querySelector("[data-detail-meta]");

  if (!detailPanel || !detailTitle || !detailKicker || !detailBody || !detailMeta) {
    return;
  }

  const closeDetailPanel = () => {
    detailPanel.classList.remove("is-open");
    detailPanel.setAttribute("aria-hidden", "true");
  };

  document.querySelectorAll(".detail-trigger").forEach((button) => {
    button.addEventListener("click", () => {
      detailTitle.textContent = button.dataset.detailTitle || "Detalle";
      detailKicker.textContent = button.dataset.detailKicker || "RCA";
      detailBody.textContent = button.dataset.detailBody || "Sin detalle cargado.";
      detailMeta.textContent = button.dataset.detailMeta || "";
      detailPanel.classList.add("is-open");
      detailPanel.setAttribute("aria-hidden", "false");
    });
  });

  document.querySelectorAll("[data-detail-close]").forEach((button) => {
    button.addEventListener("click", closeDetailPanel);
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      closeDetailPanel();
    }
  });
});
