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

  const stageWorkspace = document.querySelector("[data-rca-stage-workspace]");
  const stageTabs = stageWorkspace?.querySelectorAll("[data-rca-stage-target]") || [];
  const stageTriggers = document.querySelectorAll("[data-rca-stage-target]");
  const stagePanels = document.querySelectorAll("[data-rca-stage-panel]");

  if (stageWorkspace && stageTriggers.length > 0 && stagePanels.length > 0) {
    const stageKeys = [...stageTabs].map((tab) => tab.dataset.rcaStageTarget);
    const normalizeStage = (value) => stageKeys.find((key) => key === value) || stageWorkspace.dataset.initialRcaStage || stageKeys[0];
    const getHashStage = () => {
      if (!window.location.hash.startsWith("#stage-")) {
        return "";
      }

      return decodeURIComponent(window.location.hash.replace("#stage-", ""));
    };
    const validationPanel = document
      .querySelector("[data-rca-stage-panel] .input-validation-error, [data-rca-stage-panel] .validation-summary-errors")
      ?.closest("[data-rca-stage-panel]");

    const activateStage = (stage, updateHash = false) => {
      const activeStage = normalizeStage(stage);

      stageTabs.forEach((tab) => {
        const isActive = tab.dataset.rcaStageTarget === activeStage;
        tab.classList.toggle("is-active", isActive);
        tab.setAttribute("aria-selected", isActive ? "true" : "false");
      });

      stagePanels.forEach((panel) => {
        const isActive = panel.dataset.rcaStagePanel === activeStage;
        panel.classList.toggle("is-active", isActive);
        panel.hidden = !isActive;
      });

      if (updateHash) {
        window.history.replaceState(null, "", `#stage-${activeStage}`);
      }
    };

    activateStage(validationPanel?.dataset.rcaStagePanel || getHashStage() || stageWorkspace.dataset.initialRcaStage);

    stageTriggers.forEach((trigger) => {
      trigger.addEventListener("click", (event) => {
        event.preventDefault();
        activateStage(trigger.dataset.rcaStageTarget, true);
      });
    });
  }

  const wizardCheckTabs = document.querySelector("[data-wizard-check-tabs]");
  const wizardCheckPanels = document.querySelectorAll("[data-wizard-check-panel]");

  if (wizardCheckTabs && wizardCheckPanels.length > 0) {
    const wizardCheckButtons = wizardCheckTabs.querySelectorAll("[data-wizard-check-target]");

    const activateWizardCheck = (step) => {
      wizardCheckButtons.forEach((button) => {
        const isActive = button.dataset.wizardCheckTarget === step;
        button.classList.toggle("is-active", isActive);
        button.setAttribute("aria-selected", isActive ? "true" : "false");
      });

      wizardCheckPanels.forEach((panel) => {
        const isActive = panel.dataset.wizardCheckPanel === step;
        panel.classList.toggle("is-active", isActive);
        panel.hidden = !isActive;
      });
    };

    wizardCheckButtons.forEach((button) => {
      button.addEventListener("click", () => {
        activateWizardCheck(button.dataset.wizardCheckTarget);
      });
    });
  }

  document.querySelectorAll("form").forEach((form) => {
    form.addEventListener("submit", () => {
      form.classList.add("is-submitting");
    });
  });

  document.querySelectorAll("[data-cause-card]").forEach((card) => {
    card.addEventListener("dragstart", (event) => {
      if (event.target.closest("button, a")) {
        event.preventDefault();
        return;
      }

      card.classList.add("is-dragging");
      event.dataTransfer.effectAllowed = "move";
    });

    card.addEventListener("dragend", () => {
      card.classList.remove("is-dragging");
    });
  });

  document.querySelectorAll(".cause-list").forEach((list) => {
    list.addEventListener("dragover", (event) => {
      const draggingCard = document.querySelector(".cause-card.is-dragging");

      if (!draggingCard || draggingCard.parentElement !== list) {
        return;
      }

      event.preventDefault();
      const afterElement = [...list.querySelectorAll(".cause-card:not(.is-dragging)")]
        .find((item) => event.clientY <= item.getBoundingClientRect().top + item.offsetHeight / 2);

      if (afterElement) {
        list.insertBefore(draggingCard, afterElement);
      } else {
        list.appendChild(draggingCard);
      }
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
