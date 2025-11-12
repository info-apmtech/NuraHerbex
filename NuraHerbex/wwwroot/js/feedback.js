(function () {
    const popup = document.getElementById('feedbackPopup');
    if (!popup) return;

    const stars = popup.querySelectorAll('.star');
    const hint = popup.querySelector('.rating-hint');
    const txt = document.getElementById('fbMessage');
    const orderEl = document.getElementById('fbOrderId');
    const custEl = document.getElementById('fbCustomerId');
    const submit = document.getElementById('btnSubmitFeedback');

    const labels = ['Terrible', 'Poor', 'Okay', 'Good', 'Excellent'];
    let rating = 0;

    // Open popup from the button you showed
    document.addEventListener('click', (e) => {
        const btn = e.target.closest('.open-feedback');
        if (!btn) return;

        rating = 0;
        txt.value = '';

        // prefer data-* from button; fallback to hidden defaults
        orderEl.value = btn.dataset.orderId || orderEl.value || '1';
        custEl.value = btn.dataset.customerId || custEl.value || '304e12ef-050d-4bb2-8b3e-958a67a69850';

        setStars(0);
        hint.textContent = '';
        popup.style.display = 'block';
    });

    // Close
    document.addEventListener('click', (e) => {
        const close = e.target.closest('[data-close="#feedbackPopup"]');
        if (close) popup.style.display = 'none';
    });

    // Stars behaviour
    stars.forEach(s => {
        s.addEventListener('mouseenter', () => {
            const v = Number(s.dataset.val); setStars(v); hint.textContent = labels[v - 1] || '';
        });
        s.addEventListener('mouseleave', () => {
            setStars(rating); hint.textContent = rating ? labels[rating - 1] : '';
        });
        s.addEventListener('click', () => {
            rating = Number(s.dataset.val); setStars(rating); hint.textContent = labels[rating - 1] || '';
        });
    });
    function setStars(v) { stars.forEach(st => st.classList.toggle('is-on', Number(st.dataset.val) <= v)); }

    // Submit to API
    submit.addEventListener('click', async () => {
        if (!rating) { alert('Please select a rating.'); return; }
        const orderId = Number(orderEl.value || 0);
        const customerId = (custEl.value || '').trim(); // GUID string

        submit.disabled = true; submit.textContent = 'Submitting...';
        try {
            const res = await fetch('/Home/SubmitFeedback', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': AF_TOKEN,    // <-- REQUIRED because of [ValidateAntiForgeryToken]
                },
                body: JSON.stringify({ orderId, customerId, rating, message: txt.value || null })
            });
            if (!res.ok) throw new Error((await res.text()) || 'Failed to submit feedback');

            popup.querySelector('.cart-content').innerHTML =
                `<div style="padding:10px 0;color:#2e7d32;">Thanks for your feedback! ⭐ ${rating}/5</div>`;
            submit.textContent = 'Close';
            submit.disabled = false;
            submit.onclick = () => { popup.style.display = 'none'; location.reload?.(); };
        } catch (err) {
            alert(err.message || 'Something went wrong.');
            submit.disabled = false; submit.textContent = 'Submit';
        }
    });
})();
