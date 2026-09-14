/* ============================================================
   GAMESTORE — GAMING THEME JS
   - Hieu ung xuat hien khi cuon toi (scroll reveal)
   - Tu dong ap cho card/section ma khong can sua markup view
   - Ripple khi bam nut, tilt nhe khi hover card
   Khong phu thuoc thu vien ngoai. An toan neu chay nhieu lan.
   ============================================================ */
(function () {
    "use strict";

    var reduceMotion = window.matchMedia &&
        window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    function ready(fn) {
        if (document.readyState !== "loading") fn();
        else document.addEventListener("DOMContentLoaded", fn);
    }

    /* ---------- 1. Scroll reveal ---------- */
    function setupReveal() {
        if (reduceMotion) return;

        // Tu dong danh dau cac phan tu can hieu ung (neu chua co gx-reveal)
        var autoSelectors = [
            ".card", ".gs-card", ".gx-panel",
            ".gs-section-title", ".gx-reveal-auto"
        ];
        var nodes = document.querySelectorAll(autoSelectors.join(","));
        nodes.forEach(function (el) {
            if (!el.classList.contains("gx-reveal") && !el.dataset.gxSkip) {
                el.classList.add("gx-reveal");
            }
        });

        var targets = document.querySelectorAll(".gx-reveal:not(.gx-in)");
        if (!("IntersectionObserver" in window)) {
            // Trinh duyet cu: hien luon
            targets.forEach(function (el) { el.classList.add("gx-in"); });
            return;
        }

        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add("gx-in");
                    io.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12, rootMargin: "0px 0px -40px 0px" });

        // Them do tre so le cho cac card cung mot hang -> hieu ung stagger
        targets.forEach(function (el, i) {
            var d = (i % 4) + 1;
            el.classList.add("gx-d" + d);
            io.observe(el);
        });
    }

    /* ---------- 2. Ripple khi bam nut ---------- */
    function setupRipple() {
        document.addEventListener("click", function (e) {
            var btn = e.target.closest(".btn, .gs-btn");
            if (!btn || reduceMotion) return;
            var rect = btn.getBoundingClientRect();
            var circle = document.createElement("span");
            var size = Math.max(rect.width, rect.height);
            circle.style.cssText =
                "position:absolute;border-radius:50%;pointer-events:none;" +
                "background:rgba(255,255,255,.35);transform:scale(0);" +
                "animation:gxRipple .6s ease-out;width:" + size + "px;height:" + size + "px;" +
                "left:" + (e.clientX - rect.left - size / 2) + "px;" +
                "top:" + (e.clientY - rect.top - size / 2) + "px;";
            if (getComputedStyle(btn).position === "static") btn.style.position = "relative";
            btn.appendChild(circle);
            setTimeout(function () { circle.remove(); }, 600);
        });

        // Chen keyframe ripple 1 lan
        if (!document.getElementById("gx-ripple-style")) {
            var s = document.createElement("style");
            s.id = "gx-ripple-style";
            s.textContent = "@keyframes gxRipple{to{transform:scale(2.6);opacity:0}}";
            document.head.appendChild(s);
        }
    }

    /* ---------- 3. Tilt nhe khi hover card noi bat ---------- */
    function setupTilt() {
        if (reduceMotion) return;
        var cards = document.querySelectorAll(".gx-tilt");
        cards.forEach(function (card) {
            card.addEventListener("mousemove", function (e) {
                var r = card.getBoundingClientRect();
                var px = (e.clientX - r.left) / r.width - 0.5;
                var py = (e.clientY - r.top) / r.height - 0.5;
                card.style.transform =
                    "perspective(800px) rotateY(" + (px * 6) + "deg) rotateX(" + (-py * 6) + "deg) translateY(-6px)";
            });
            card.addEventListener("mouseleave", function () {
                card.style.transform = "";
            });
        });
    }

    ready(function () {
        setupReveal();
        setupRipple();
        setupTilt();
    });
})();
