(function () {
    function getUrls() {
        var $modal = $('#cartModal');
        return {
            partialUrl: $modal.data('partialUrl') || '/Home/_ShoppingCartPartial',
            addUrl: $modal.data('addUrl') || '/Home/Cartlist'
        };
    }

    function openCartModal() {
        var urls = getUrls();
        var $modal = $('#cartModal');
        var $content = $('#cartPopupContent');

        if (!$modal.length || !$content.length) {
            console.warn('[cart] #cartModal / #cartPopupContent not found.');
            return;
        }

        $content.empty();
        $modal.fadeIn(200);

        $.get(urls.partialUrl)
            .done(function (html) { $content.html(html); })
            .fail(function () {
                $content.html('<div style="padding:20px;color:#c00">Failed to load cart.</div>');
            });
    }

    function reloadCartPartialIntoModal() {
        var urls = getUrls();
        var $content = $('#cartPopupContent');
        if (!$content.length) return;

        $.get(urls.partialUrl)
            .done(function (html) { $content.html(html); })
            .fail(function () {
                $content.html('<div style="padding:20px;color:#c00">Failed to reload cart.</div>');
            });
    }

    // Add to cart
    $(document)
        .off('click.cart.add')
        .on('click.cart.add', '.btn-add-cart', function (e) {
            e.preventDefault();

            var productId = $(this).data('productId');
            if (!productId) {
                alert('Invalid product.');
                return;
            }

            var urls = getUrls();

            $.ajax({
                type: 'POST',
                url: urls.addUrl,
                data: { productId: productId }
            })
                .done(function () {
                    openCartModal(); // open on success
                    reloadCartPartialIntoModal();
                })
                .fail(function (xhr) {
                    // 🧠 If user is not logged in
                    if (xhr.status === 401) {
                        $('#loginModal').fadeIn(200);
                    } else {
                        alert('Failed to add to cart.');
                    }
                });
        });

    // Remove from cart
    $(document)
        .off('click.cart.remove')
        .on('click.cart.remove', '.remove-link', function (e) {
            e.preventDefault();
            const cartId = $(this).data('cartid');
            if (!cartId) return alert('Invalid item.');

            $.ajax({
                type: 'DELETE',
                url: `/AdminAPI/Cart/${cartId}`
            })
                .done(function () {
                    reloadCartPartialIntoModal();
                })
                .fail(function (xhr) {
                    alert('Failed to remove item: ' + (xhr.responseText || 'Error'));
                });
        });

    // Close modals
    $(document)
        .on('click.cart.closebtn', '.close-btn', function () {
            $('#cartModal').fadeOut(200);
        })
        .on('click.login.cancel', '#btnLoginCancel', function () {
            $('#loginModal').fadeOut(200);
        });

    // Handle login form
    // --- LOGIN MODAL HANDLERS ---

    // openLoginModal(): call this from your add-to-cart failure (401) path
    function openLoginModal() {
        $("#loginError").hide().text("");
        $("#loginModal").fadeIn(180).attr("aria-hidden", "false");
    }

    function closeLoginModal() {
        $("#loginModal").fadeOut(160).attr("aria-hidden", "true");
    }

    // Close on X or Cancel buttons or clicking overlay
    $(document)
        .off("click.login.cancel")
        .on("click.login.cancel", "#btnLoginCancel,#btnLoginCancel2", function () {
            closeLoginModal();
        });

    $("#loginModal")
        .off("click.login.backdrop")
        .on("click.login.backdrop", function (e) {
            if (!$(e.target).closest(".modal-content").length) closeLoginModal();
        });

    // Handle login submit (use form submit so Enter key works)
    $(document)
        .off("submit.login.form")
        .on("submit.login.form", "#loginForm", function (e) {
            e.preventDefault();

            var $form = $(this);
            var username = $form.find('[name="Username"]').val().trim();
            var password = $form.find('[name="Password"]').val().trim();
            var token = $form.find('input[name="__RequestVerificationToken"]').val();

            if (!username || !password) {
                $("#loginError").text("Please enter username and password.").show();
                return;
            }

            $.ajax({
                type: "POST",
                url: $form.attr("action") || "/Authentication/SignIn",
                data: { Username: username, Password: password },
                headers: { "RequestVerificationToken": token } // ASP.NET Core anti-forgery header
            })
                .done(function () {
                    $("#loginError").hide();
                    closeLoginModal();
                    // refresh UI so ClaimTypes.NameIdentifier becomes available
                    //location.reload();
                })
                .fail(function (xhr) {
                    var msg =
                        (xhr.responseJSON && (xhr.responseJSON.message || xhr.responseJSON.error)) ||
                        xhr.responseText ||
                        "Invalid username or password.";
                    $("#loginError").text(msg).show();
                });
        });


})();
// --- INCREMENT (+) ---
$(document)
    .off('click.cart.qty.plus')
    .on('click.cart.qty.plus', '.qty-btn.plus', function (e) {
        e.preventDefault();

        var productId = $(this).data('productid');
        if (!productId) return;

        // Your existing "add" endpoint increments quantity if item exists
        $.ajax({
            type: 'POST',
            url: '/Home/Cartlist',
            data: { productId: productId }
        })
            .done(function () {
                // refresh the cart partial in the modal
                if (typeof reloadCartPartialIntoModal === 'function') reloadCartPartialIntoModal();
            })
            .fail(function (xhr) {
                if (xhr.status === 401 && window.showLoginModal) showLoginModal(); // optional
                else alert('Failed to increase quantity.');
            });
    });

//// --- DECREMENT (−) ---
//$(document)
//    .off('click.cart.qty.minus')
//    .on('click.cart.qty.minus', '.qty-btn.minus', function (e) {
//        e.preventDefault();

//        var $row = $(this).closest('.cart-content');
//        var cartId = $row.data('cartid');
//        var current = parseInt($row.find('.qty-val').text(), 10) || 1;

//        if (!cartId) return;

//        // If qty would go to 0, delete the line
//        if (current <= 1) {
//            $.ajax({ type: 'DELETE', url: '/AdminAPI/Cart/' + cartId })
//                .done(function () {
//                    if (typeof reloadCartPartialIntoModal === 'function') reloadCartPartialIntoModal();
//                })
//                .fail(function () { alert('Failed to remove item.'); });
//            return;
//        }

//        // Otherwise decrement by 1 via PATCH (see API in section 3)
//        $.ajax({
//            type: 'PATCH',
//            url: '/AdminAPI/Cart/' + cartId + '/quantity',
//            contentType: 'application/json; charset=utf-8',
//            data: JSON.stringify({ delta: -1 })
//        })
//            .done(function () {
//                if (typeof reloadCartPartialIntoModal === 'function') reloadCartPartialIntoModal();
//            })
//            .fail(function () { alert('Failed to decrease quantity.'); });
//    });
// ----------- Quantity Controls: Instant UI Update -----------

function updateCartTotals() {
    let subtotal = 0;
    $(".cart-content").each(function () {
        const qty = parseInt($(this).find(".qty-val").text()) || 0;
        const price = parseFloat($(this).find(".product-price").text().replace(/[^\d.]/g, "")) || 0;
        subtotal += qty * price;
    });
    $(".subtotal-value").text("₹" + subtotal.toFixed(2));
}

function toggleLeftButton($row, qty) {
    const $left = $row.find(".qty-btn").first();
    if (qty > 1) {
        $left.removeClass("delete").addClass("minus").text("−");
    } else {
        $left.removeClass("minus").addClass("delete").text("🗑");
    }
}

// ----------- INCREMENT (+) -----------
$(document)
    .off("click.cart.qty.plus")
    .on("click.cart.qty.plus", ".qty-btn.plus", function (e) {
        e.preventDefault();
        const $row = $(this).closest(".cart-content");
        const $qty = $row.find(".qty-val");
        const cartId = $(this).data("cartid");
        if (!cartId) return;

        const oldQty = parseInt($qty.text()) || 1;
        const newQty = oldQty + 1;

        // Instant UI update
        $qty.text(newQty);
        toggleLeftButton($row, newQty);
        updateCartTotals();

        // Background server update
        $.ajax({
            type: "POST",
            url: "/Home/ChangeQuantity",
            data: { cartId: cartId, delta: 1 }
        })
            .done(function () {
                // Optional: re-sync from server if needed
                if (typeof reloadCartPartialIntoModal === "function") reloadCartPartialIntoModal();
            })
            .fail(function () {
                // Rollback if server fails
                $qty.text(oldQty);
                toggleLeftButton($row, oldQty);
                updateCartTotals();
                alert("Failed to increase quantity.");
            });
    });

// ----------- DECREMENT (−) -----------
$(document)
    .off("click.cart.qty.minus")
    .on("click.cart.qty.minus", ".qty-btn.minus", function (e) {
        e.preventDefault();
        const $row = $(this).closest(".cart-content");
        const $qty = $row.find(".qty-val");
        const cartId = $(this).data("cartid");
        if (!cartId) return;

        const oldQty = parseInt($qty.text()) || 1;
        if (oldQty <= 1) return;

        const newQty = oldQty - 1;

        // Instant UI update
        $qty.text(newQty);
        toggleLeftButton($row, newQty);
        updateCartTotals();

        // Background server update
        $.ajax({
            type: "POST",
            url: "/Home/ChangeQuantity",
            data: { cartId: cartId, delta: -1 }
        })
            .done(function () {
                if (typeof reloadCartPartialIntoModal === "function") reloadCartPartialIntoModal();
            })
            .fail(function () {
                // Rollback if server fails
                $qty.text(oldQty);
                toggleLeftButton($row, oldQty);
                updateCartTotals();
                alert("Failed to decrease quantity.");
            });
    });

// ----------- DELETE (🗑) -----------
$(document)
    .off("click.cart.qty.delete")
    .on("click.cart.qty.delete", ".qty-btn.delete", function (e) {
        e.preventDefault();
        const $row = $(this).closest(".cart-content");
        const cartId = $(this).data("cartid");
        if (!cartId) return;

        const backup = $row.clone(true, true); // for rollback
        $row.fadeOut(150, function () {
            $(this).remove();
            updateCartTotals();
        });

        $.ajax({
            type: "DELETE",
            url: "/Home/DeleteCartItem",
            data: { cartId: cartId }
        })
            .done(function () {
                if (typeof reloadCartPartialIntoModal === "function") reloadCartPartialIntoModal();
            })
            .fail(function () {
                // rollback
                $(".cart-footer").before(backup);
                alert("Failed to remove item.");
                updateCartTotals();
            });
    });



