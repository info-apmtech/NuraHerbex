// wwwroot/js/cart.js

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

    // Add to cart (delegated)
    $(document)
        .off('click.cart.add')
        .on('click.cart.add', '.btn-add-cart', function (e) {
            e.preventDefault();

            // Open popup immediately so the user sees it
            openCartModal();

            // product id from data-product-id (camelCase in jQuery)
            var productId = $(this).data('productId');
            if (!productId) {
                $('#cartPopupContent').html(
                    '<div class="cart-popup">' +
                    '<div class="cart-header"><span>Shopping Cart</span><span class="close-btn" aria-label="Close">&times;</span></div>' +
                    '<div class="cart-items" style="padding:18px;color:#c00">Invalid product. Please try again.</div>' +
                    '</div>'
                );
                return;
            }

            var urls = getUrls();

            // Talk to your HomeController Cartlist(int productId) (it uses Claims user server-side)
            $.ajax({
                type: 'POST',
                url: urls.addUrl,
                data: { productId: productId }
            })
                .always(function () {
                    reloadCartPartialIntoModal();
                })
                .fail(function (xhr) {
                    var msg = (xhr && xhr.responseText) ? xhr.responseText : 'Failed to add to cart.';
                    console.warn('[cart] add failed:', msg);
                });
        });

    // Floating Cart button opens the modal
    $(document)
        .off('click.cart.fab')
        .on('click.cart.fab', '.floating-btn[aria-label="Cart"]', function () {
            openCartModal();
        });

    // Close when clicking outside modal content
    $('#cartModal')
        .off('click.cart.backdrop')
        .on('click.cart.backdrop', function (e) {
            if (!$(e.target).closest('.modal-content').length) {
                $('#cartModal').fadeOut(200);
            }
        });

    // Close when clicking any .close-btn
    $(document)
        .off('click.cart.closebtn')
        .on('click.cart.closebtn', '.close-btn', function () {
            $('#cartModal').fadeOut(200);            // close modal
            $(this).closest('.cart-popup').addClass('hidden'); // also hide inline block if present
        });

    // Close on Esc
    $(document)
        .off('keyup.cart.esc')
        .on('keyup.cart.esc', function (e) {
            if (e.key === 'Escape') { $('#cartModal').fadeOut(200); }
        });
})();
