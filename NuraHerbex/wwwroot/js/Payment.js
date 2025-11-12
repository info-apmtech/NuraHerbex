function razorpayPaymentConfirm() {
    var paymentAmount = $('#VehicleOwnerInfo_PaymentAmount').val();
    var customerName = $('#VehicleOwnerInfo_Ownername').val();
    var createdAt = new Date();

    var payload = {
        Amount: parseFloat(paymentAmount)
    };

    $.ajax({
        type: "POST",
        url: "https://payment.tracole.com/payment/create-order", // RazorpayIntegrationProjectAPI URL
        //url: "https://localhost:7106/payment/create-order", // RazorpayIntegrationProjectAPI URL
        contentType: "application/json",
        data: JSON.stringify(payload),
        success: function (response) {
            if (response.id) { // Razorpay order ID
                var razorpayOptions = {
                    key: response.key, // Razorpay Key
                    amount: response.amount,
                    currency: response.currency,
                    order_id: response.id,
                    name: customerName,
                    handler: function (paymentResponse) {
                        // Verify payment
                        var verifyPayload = {
                            PaymentId: paymentResponse.razorpay_payment_id,
                            OrderId: paymentResponse.razorpay_order_id,
                            Signature: paymentResponse.razorpay_signature
                        };

                        $.ajax({
                            type: "POST",
                            url: "https://payment.tracole.com/payment/verify-payment", // RazorpayIntegrationProjectAPI URL
                            //url: "https://localhost:7106/payment/verify-payment", // RazorpayIntegrationProjectAPI URL
                            contentType: "application/json",
                            data: JSON.stringify(verifyPayload),
                            success: function (verificationResponse) {
                                if (verificationResponse.message === "Payment verified successfully") {
                                    toastr.success("Payment Successful!", "Success");
                                    // Additional logic for post-payment actions
                                } else {
                                    toastr.error("Payment Verification Failed!", "Error");
                                }
                            },
                            error: function () {
                                toastr.error("Error verifying payment!", "Error");
                            }
                        });
                    },
                    prefill: {
                        name: customerName
                    }
                };

                var razorpayInstance = new Razorpay(razorpayOptions);
                razorpayInstance.open();
            } else {
                toastr.error("Failed to create order!", "Error");
            }
        },
        error: function () {
            toastr.error("Failed to process payment!", "Error");
        }
    });
}
