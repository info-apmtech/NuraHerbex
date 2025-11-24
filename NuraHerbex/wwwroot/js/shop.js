(function () {
    const fmtINR = new Intl.NumberFormat('en-IN');

    // ---------- helpers ----------
    function escapeHtml(s) {
        return (s || '').replace(/[&<>"']/g, m => (
            { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[m]
        ));
    }

    function splitImages(s) {
        return (s || '').split(';').map(x => x.trim()).filter(Boolean);
    }

    function splitHeadingDesc(s) {
        const str = s || '';
        const i = str.indexOf('|');
        return i >= 0 ? [str.slice(0, i).trim(), str.slice(i + 1).trim()] : [str.trim(), ''];
    }

    // SVG icon pickers (same logic you use server-side)
    const icons = {
        focus: () => `
    <svg viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <circle cx="16" cy="16" r="10" />
      <circle cx="16" cy="16" r="4" />
      <circle cx="16" cy="16" r="1.5" class="fill" />
    </svg>`,

        energy: () => `
    <svg viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <polyline points="14,3 5,17 15,17 12,29 27,13 17,13 18,3"
        stroke-linecap="round" stroke-linejoin="round" />
    </svg>`,

        shield: () => `
    <svg viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <path d="M16 4 L26 8 V16c0 7-6 11-10 12C8 27 6 23 6 16V8Z"/>
    </svg>`,

        clock: () => `
    <svg viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <circle cx="16" cy="16" r="12" />
      <line x1="16" y1="16" x2="22" y2="16" />
      <line x1="16" y1="10" x2="16" y2="16" />
    </svg>`,

        user: () => `
    <svg viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <circle cx="16" cy="13" r="6" />
      <path d="M7 27c0-5 6-6 9-6s9 1 9 6" />
    </svg>`,

        target: () => `
    <svg viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <circle cx="16" cy="16" r="10" />
      <circle cx="16" cy="16" r="4" />
      <circle cx="16" cy="16" r="1.5" class="fill" />
    </svg>`,

        trophy: () => `
    <svg viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <rect x="11" y="8" width="10" height="8" rx="2" />
      <path d="M14 16v3a2 2 0 1 0 4 0v-3" />
      <path d="M11 13.5c-1.7-.3-3.2-1.3-3.2-2.8V9" />
      <path d="M21 13.5c1.7-.3 3.2-1.3 3.2-2.8V9" />
    </svg>`
    };


    function benefitIconFor(text) {
        const t = (text || '').toLowerCase();
        if (t.includes('focus') || t.includes('clarity') || t.includes('concentr')) return icons.focus();
        if (t.includes('energy') || t.includes('boost') || t.includes('power')) return icons.energy();
        if (t.includes('antioxid') || t.includes('immune') || t.includes('protect')) return icons.shield();
        if (t.includes('stamina') || t.includes('endurance') || t.includes('all-day') || t.includes('all day')) return icons.clock();
        return icons.clock();
    }

    function whoIconFor(text) {
        const t = (text || '').toLowerCase();
        if (t.includes('professional') || t.includes('busy') || t.includes('executive')) return icons.user();
        if (t.includes('health') || t.includes('beginner') || t.includes('conscious')) return icons.target();
        if (t.includes('daily') || t.includes('energy') || t.includes('active')) return icons.trophy();
        return icons.user();
    }

    // Build UI for benefits from AdminAPI fields keyBenefits1..4
    function rebuildBenefitsFromApiProduct(p) {
        const wrap = document.getElementById('benefitsGrid');
        if (!wrap) return;

        const rows = [p.keyBenefits1, p.keyBenefits2, p.keyBenefits3, p.keyBenefits4].filter(Boolean);
        if (!rows.length) {
            wrap.innerHTML = `<p class="text-muted">No benefits found for this product.</p>`;
            return;
        }

        const cards = rows.map(s => {
            const [head, desc] = splitHeadingDesc(s);
            const safeH = escapeHtml(head);
            const safeD = escapeHtml(desc);
            return `<div class="benefit-card">
                    <div class="benefit-icon">${benefitIconFor(head)}</div>
                    <div class="benefit-title">${safeH}</div>
                    <div class="benefit-desc">${safeD}</div>
                  </div>`;
        }).join('');

        wrap.innerHTML = `<div class="benefits-grid">${cards}</div>`;
    }

    // Build UI for who from AdminAPI fields forThis1..4
    function rebuildWhoFromApiProduct(p) {
        const wrap = document.getElementById('whoGrid');
        if (!wrap) return;

        const rows = [p.forThis1, p.forThis2, p.forThis3, p.forThis4].filter(Boolean);
        if (!rows.length) {
            wrap.innerHTML = `<p class="text-muted">No audience information available for this product.</p>`;
            return;
        }

        const cards = rows.map(s => {
            const [head, desc] = splitHeadingDesc(s);
            const safeH = escapeHtml(head);
            const safeD = escapeHtml(desc).replace(/\n/g, '<br>');
            return `<div class="who-card">
                    <div class="who-icon">${whoIconFor(head)}</div>
                    <div class="who-head">${safeH}</div>
                    <div class="who-desc">${safeD}</div>
                  </div>`;
        }).join('');

        wrap.innerHTML = `<div class="who-grid">${cards}</div>`;
    }

    // ---------- existing elements ----------
    const switcher = document.getElementById('productSwitcher');
    const gallery = document.getElementById('productGallery');
    const mainImg = document.getElementById('mainImg');
    const nameEl = document.getElementById('prodName');
    const descEl = document.getElementById('prodDesc');
    const priceRow = document.getElementById('priceRow');
    const buyBtn = document.getElementById('buyBtn');

    // Under-main gallery: swap only the hero image
    if (gallery) {
        gallery.addEventListener('click', (e) => {
            const img = e.target.closest('img');
            if (!img) return;
            gallery.querySelectorAll('img').forEach(x => x.classList.remove('active'));
            img.classList.add('active');
            mainImg.src = img.src;
            mainImg.alt = img.alt || mainImg.alt;
        });
    }

    // Right column: switch whole product via AdminAPI/product/{id}
    if (switcher) {
        switcher.addEventListener('click', async (e) => {
            const thumb = e.target.closest('img[data-product-id]');
            if (!thumb) return;

            switcher.querySelectorAll('img').forEach(x => x.classList.remove('active'));
            thumb.classList.add('active');

            const res = await fetch(`/Home/GetProduct?id=${encodeURIComponent(thumb.dataset.productId)}`, {
                headers: { 'Accept': 'application/json' }
            });
            if (!res.ok) { alert('Product not found.'); return; }

            const p = await res.json();


            // hero/name/desc/price
            const imgs = splitImages(p.productImages);
            const primary = imgs[0] || '/image/shop/Stamix1.png';
            const amount = p.amount || 0;
            const discount = p.discountPercentage || 0;
            const finalAmt = amount - (amount * (discount / 100));

            mainImg.src = primary;
            mainImg.alt = p.productName;
            nameEl.textContent = p.productName;
            descEl.textContent = (p.description && p.description.trim()) ? p.description : (p.subTitle || '');

            priceRow.innerHTML = discount > 0
                ? `<span class="shop-price">₹ ${fmtINR.format(finalAmt)} <span class="unit-tag">/per box</span></span>
               <span class="shop-price-compare"><s>₹ ${fmtINR.format(amount)}</s> <span class="shop-discount">(${discount}%)</span></span>`
                : `<span class="shop-price">₹ ${fmtINR.format(amount)} <span class="unit-tag">/per box</span></span>`;

            buyBtn.onclick = function () {
                location.href = '/Shop/Checkout?id=' + encodeURIComponent(p.id);
            };

            // rebuild under-main gallery
            gallery.innerHTML = '';
            imgs.forEach((src, i) => {
                const t = document.createElement('img');
                t.src = src;
                t.alt = p.productName;
                if (i === 0) t.classList.add('active');
                gallery.appendChild(t);
            });

            // ✅ rebuild Benefits & Who with icons
            rebuildBenefitsFromApiProduct(p);
            rebuildWhoFromApiProduct(p);
            if (typeof updatePotentialPriceCard === 'function') {
                updatePotentialPriceCard(p);
            }
        });
    }
})();
(function initPotentialCard() {
    const card = document.getElementById('potentialPriceCard');
    if (!card) return;

    const fmtINR = new Intl.NumberFormat('en-IN');
    const maxQty = parseInt(card.dataset.max || '10', 10);

    // Elements (scoped)
    const priceCurrentEl = card.querySelector('#pot_priceCurrent');
    const priceOldEl = card.querySelector('#pot_priceOld');
    const priceOffEl = card.querySelector('#pot_priceOff');
    const priceTotalEl = card.querySelector('#pot_priceTotal');

    const qtyMinus = card.querySelector('#pot_qtyMinus');
    const qtyPlus = card.querySelector('#pot_qtyPlus');
    const qtyValue = card.querySelector('#pot_qtyValue');

    // State
    let unitMrp = 0;      // original MRP
    let unitDisc = 0;     // percentage
    let unitFinal = 0;    // discounted price
    let qty = 1;
    const MIN_QTY = 1;

    function clamp(n, min, max) { return Math.max(min, Math.min(max, n)); }

    function refreshQtyUI() {
        qtyValue.textContent = String(qty);
        qtyMinus.disabled = qty <= MIN_QTY;
        qtyPlus.disabled = qty >= maxQty;
        priceTotalEl.textContent = `₹ ${fmtINR.format(unitFinal * qty)}`;
    }

    // Public updater: call this when product changes
    window.updatePotentialPriceCard = function (productJson) {
        // supports both shapes: {amount, discountPct} or {amount, discountPercentage}
        const amount = Number(productJson.amount || 0);
        const disc = Number(productJson.discountPct ?? productJson.discountPercentage ?? 0);
        const final = amount - (amount * (disc / 100));

        unitMrp = amount;
        unitDisc = disc;
        unitFinal = final;

        priceCurrentEl.textContent = fmtINR.format(unitFinal);

        if (unitDisc > 0) {
            priceOldEl.style.display = '';
            priceOffEl.style.display = '';
            priceOldEl.textContent = `₹ ${fmtINR.format(unitMrp)}`;
            priceOffEl.textContent = `${unitDisc} % OFF`;
        } else {
            priceOldEl.style.display = 'none';
            priceOffEl.style.display = 'none';
        }

        qty = 1;
        refreshQtyUI();
    };

    // Initial values pulled from server-rendered HTML:
    (function bootFromMarkup() {
        const current = Number(priceCurrentEl.textContent.replace(/[^0-9.]/g, '') || 0);
        const oldTxt = priceOldEl && priceOldEl.style.display !== 'none' ? priceOldEl.textContent : '';
        const old = Number((oldTxt || '').replace(/[^0-9.]/g, '') || 0);
        const offTxt = priceOffEl && priceOffEl.style.display !== 'none' ? priceOffEl.textContent : '';
        const off = Number((offTxt || '').replace(/[^0-9.]/g, '') || 0);

        unitFinal = current || 0;
        unitMrp = old || unitFinal;
        unitDisc = off || (unitMrp ? Math.round((1 - unitFinal / unitMrp) * 100) : 0);
        qty = 1;
        refreshQtyUI();
    })();

    // Handlers
    qtyMinus.addEventListener('click', () => { qty = clamp(qty - 1, MIN_QTY, maxQty); refreshQtyUI(); });
    qtyPlus.addEventListener('click', () => { qty = clamp(qty + 1, MIN_QTY, maxQty); refreshQtyUI(); });

})();

// BUY NOW – add to cart then show only that product in OrderSummary
$(document)
    .off("click.cart.buynow")
    .on("click.cart.buynow", ".shop-buy-btn", function (e) {
        e.preventDefault();

        var productId = $(this).data("productId");
        if (!productId) {
            alert("Invalid product.");
            return;
        }

        $.ajax({
            type: "POST",
            url: "/Home/Cartlist",          // your existing Add-to-cart
            data: { productId: productId },
            statusCode: {
                401: function () {
                    if (typeof openLoginModal === "function") {
                        openLoginModal();
                    } else {
                        $("#loginModal").fadeIn(200);
                    }
                }
            }
        })
            .done(function () {
                // ✅ OrderSummary will filter Items to only this product
                window.location.href = "/Home/OrderSummary?productId=" + productId;
            })
            .fail(function (xhr) {
                if (xhr.status === 401) {
                    if (typeof openLoginModal === "function") {
                        openLoginModal();
                    } else {
                        $("#loginModal").fadeIn(200);
                    }
                } else {
                    alert("Failed to process Buy Now.");
                }
            });
    });

