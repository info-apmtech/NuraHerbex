
function razorpayIntegrationPaymentConfirm(hasSticker) {
    // ===== 1. Get total amount from the #total span =====
    // Example innerText: "₹1,234.00"
    var totalEl = document.getElementById('total');
    var totalText = totalEl ? totalEl.innerText : '';

    // Remove everything except digits, dot, minus: "₹1,234.00" -> "1234.00"
    var cleaned = totalText.replace(/[^\d.-]/g, '');
    var paymentAmount = parseFloat(cleaned) || 0; // in ₹

    console.log('Total text:', totalText);
    console.log('Cleaned:', cleaned);
    console.log('paymentAmount (₹):', paymentAmount);

    if (!paymentAmount || paymentAmount <= 0) {
        if (window.toastr) {
            toastr.error("Invalid payment amount.", "Error");
        } else {
            alert("Invalid payment amount.");
        }
        return;
    }

    // ===== 2. Get customer details from the address form =====
    var firstName = document.getElementById('firstName')?.value || "";
    var lastName = document.getElementById('lastName')?.value || "";
    var ownerName = (firstName + " " + lastName).trim() || "Customer";
    var email = document.getElementById('email')?.value || "";
    var phoneNumber = document.getElementById('phone')?.value || "";
    var address = document.getElementById('streetAddress')?.value || "";

    var domain = "NURA"; // must match what your backend expects

    // The form that posts to ProceedToPayment
    var form = document.querySelector('.proceed-section form');
    if (!form) {
        console.error("ProceedToPayment form not found by id.");
        return;
    }

    // ===== 3. Call your existing create-order API (cannot change backend) =====
    // Signature: CreateOrder([FromBody] AmountRequest model)
    // -> model.Amount (decimal), model.DomainName (string)
    fetch('https://payment.tracole.com/payment/create-order', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            amount: paymentAmount,   // decimal in RUPEES (NOT * 100)
            domainName: domain
        })
    })
        .then(async response => {
            const raw = await response.text();
            console.log('create-order HTTP status:', response.status);
            console.log('create-order raw response:', raw);

            if (!response.ok) {
                // This will send us to the catch() below
                throw new Error("HTTP " + response.status + ": " + raw);
            }

            let order;
            try {
                order = raw ? JSON.parse(raw) : null;
            } catch (e) {
                console.error('JSON parse error for create-order:', e);
                throw new Error('Invalid JSON from create-order: ' + raw);
            }
            return order;
        })
        .then(order => {
            console.log('Parsed order object:', order);

            // Your backend returns whatever _razorpayService.CreateOrder(...) returns.
            // In your previous project, you used order.id, so we keep that assumption.
            if (!(order && order.id)) {
                if (window.toastr) {
                    toastr.error("Order response missing id from payment service.", "Error");
                } else {
                    alert("Order response missing id from payment service.");
                }
                return;
            }

            // ===== 4. Configure Razorpay checkout =====
            var options = {
                key: "rzp_live_UBscLASQP7fAT4", // Tracole Production Razorpay Key
                // key: "rzp_test_jSPTP0C4WotdSJ", // Use this for testing if needed

                amount: paymentAmount * 100,   // paise (Razorpay expects paise)
                currency: "INR",
                name: "Tracole Technologies",
                description: "Order Payment",
                image: "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSif0XXHotoAg5YT0ltoVrNIfqwKrb5KLWwOo0ITuphqh7qrwnEdVe-aecCnV6Ai8l0awI&usqp=CAU",
                order_id: order.id,           // Razorpay order id from backend

                handler: function (response) {
                    console.log('razorpay response', response);

                    var paymentData = {
                        paymentId: response.razorpay_payment_id,
                        orderId: response.razorpay_order_id,
                        signature: response.razorpay_signature,
                        domainName: domain,
                        amount: paymentAmount
                    };

                    // ===== 5. Verify payment with your existing verify-payment API =====
                    fetch('https://payment.tracole.com/payment/verify-payment', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(paymentData)
                    })
                        .then(res => res.json())
                        .then(verificationResult => {
                            console.log('Payment data', paymentData);
                            console.log('Payment verification result:', verificationResult);

                            if (verificationResult.message === "Payment verified successfully") {

                                // Mark as paid
                                document.getElementById('CustomerInfo_hasPaid').value = "true";

                                // Save payment info for server (optional)
                                document.getElementById('RazorpayPaymentId').value = paymentData.paymentId;
                                document.getElementById('RazorpayOrderId').value = paymentData.orderId;
                                document.getElementById('RazorpaySignature').value = paymentData.signature;

                                if (window.toastr) {
                                    toastr.success("Payment Successful!", "Success");
                                }

                                // ===== 6. Submit MVC form to ProceedToPayment =====
                                form.submit();
                            } else {
                                document.getElementById('CustomerInfo_hasPaid').value = "false";
                                if (window.toastr) {
                                    toastr.error("Payment verification failed.", "Error");
                                } else {
                                    alert("Payment verification failed.");
                                }
                            }
                        })
                        .catch(error => {
                            console.error('Error verifying payment:', error);
                            if (window.toastr) {
                                toastr.error("Error during payment verification.", "Error");
                            } else {
                                alert("Error during payment verification.");
                            }
                        });
                },
                prefill: {
                    name: ownerName,
                    email: email,
                    contact: phoneNumber
                },
                notes: {
                    address: address
                },
                theme: {
                    color: "#F37254"
                }
            };

            var rzp = new Razorpay(options);
            rzp.open();
        })
        .catch(error => {
            console.error('Error creating order in the other project:', error);
            if (window.toastr) {
                toastr.error("Error creating payment order.", "Error");
            } else {
                alert("Error creating payment order.");
            }
        });
}
//async function razorpayIntegrationPaymentConfirm(hasSticker) {
//    const ctx = window.PaymentContext || {};
//    const { orderId, customerId, amountPaise, amountRupees, domain } = ctx;

//    if (!orderId || !customerId || !amountPaise) {
//        toastr.error("Payment context missing. Refresh the page.", "Error");
//        return;
//    }

//    // 1) Create Razorpay order via payment.tracole (expects RUPEES)
//    let created;
//    try {
//        const res = await fetch('https://payment.tracole.com/payment/create-order', {
//            method: 'POST',
//            headers: { 'Content-Type': 'application/json' },
//            body: JSON.stringify({
//                amount: amountPaise / 100,  // rupees
//                domainName: domain,
//                metadata: { orderId, customerId }
//            })
//        });
//        created = await res.json();     // expect { id: "<rzp_order_...>" }
//        if (!created?.id) throw new Error('Invalid create-order response');
//    } catch (e) {
//        console.error(e);
//        toastr.error("Error creating payment order.", "Error");
//        return;
//    }

//    // 2) Open Razorpay (fixed publishable key)
//    const rzp = new Razorpay({
//        key: "rzp_live_UBscLASQP7fAT4", // <-- using your existing live key
//        amount: amountPaise,            // paise
//        currency: "INR",
//        name: "Tracole Technologies",
//        description: "Order Payment",
//        order_id: created.id,
//        handler: async function (response) {
//            try {
//                // 3) Verify via payment.tracole (expects RUPEES)
//                const verifyRes = await fetch('https://payment.tracole.com/payment/create-order', {
//                    method: 'POST',
//                    headers: { 'Content-Type': 'application/json' },
//                    body: JSON.stringify({
//                        paymentId: response.razorpay_payment_id,
//                        orderId: response.razorpay_order_id,
//                        signature: response.razorpay_signature,
//                        domainName: domain,
//                        amount: amountPaise / 100
//                    })
//                });
//                const verificationResult = await verifyRes.json();

//                if (verificationResult?.message === "Payment verified successfully") {
//                    document.getElementById('CustomerInfo_hasPaid')?.setAttribute('value', 'true');
//                    toastr.success("Payment Successful!", "Success");
//                    fetchPaymentDetails(response.razorpay_payment_id, hasSticker);
//                } else {
//                    document.getElementById('CustomerInfo_hasPaid')?.setAttribute('value', 'false');
//                    toastr.error("Payment verification failed.", "Error");
//                }
//            } catch (err) {
//                console.error(err);
//                toastr.error("Error during payment verification.", "Error");
//            }
//        },
//        // Prefill from server if you render them:
//        // prefill: { name: '@Model.CustomerName', email: '@Model.Email', contact: '@Model.Phone' },
//        notes: {
//            app_order_id: String(orderId),
//            customer_id: String(customerId),
//            amount_rupees: String(amountRupees ?? (amountPaise / 100))
//        },
//        theme: { color: "#F37254" }
//    });

//    rzp.open();
//}
//function fetchPaymentDetails(paymentId, hasSticker) {


//    fetch(`https://payment.tracole.com/payment/fetch-payment/${paymentId}`, {
//        //fetch(`https://localhost:7106/payment/fetch-payment/${paymentId}`, {
//        method: "GET",
//        headers: {
//            "Accept": "application/json"
//        }
//    })
//        .then(response => {
//            if (!response.ok) {
//                throw new Error(`HTTP error! Status: ${response.status}`);
//            }
//            return response.json();
//        })
//        .then(fetchedPaymentDetails => {
//            console.log("Received Payment Details:", fetchedPaymentDetails);

//            if (!fetchedPaymentDetails || Object.keys(fetchedPaymentDetails).length === 0) {
//                toastr.error("Received empty payment details!", "Error");
//                return;
//            }

//            if (hasSticker === 'true') {
//                paymentConfirmSticker(fetchedPaymentDetails);
//            } else {
//                paymentConfirmSuccess(fetchedPaymentDetails);
//            }
//        })
//        .catch(error => {
//            console.error("Error fetching payment details:", error);
//            toastr.error("Error fetching payment details.", "Error");
//        });
//}

//function paymentConfirmSuccess(paymentData) {
//    var paymentDetails = JSON.stringify(paymentData);
//    console.log('fetchpayment', paymentDetails);
//    var orderid = 0;
//    var appointmentType = $('#HSRPAppointment_AppointmentType').val();
//    console.log('apptype', appointmentType);
//    var createdAt = new Date();
//    var isSticker = false;

//    $('#HSRPOrder_CreatedAt').val(createdAt);
//    var hSRPAppointment = {
//        AppointmentId: parseInt($('#HSRPAppointment_AppointmentId').val()),
//        OrderId: orderid,
//        AppointmentType: getAppointmentTypeEnumValue(appointmentType),
//        CreatedAt: createdAt,
//        hasSticker: isSticker
//    }
//    console.log('hsrpapp', hSRPAppointment);
//    var deliveryNoteId = 0;
//    var embossingissueDate = new Date('0001-01-01');
//    console.log('embdate', embossingissueDate);
//    var entrydate = new Date();
//    var hotFoilingDate = new Date('0001-01-01');
//    var invoiceGenerateDate = new Date('0001-01-01');
//    var orderDate = new Date();
//    var regdate = $('#HSRPOrder_RegDate').val();
//    var mandate = $('#HSRPOrder_DateofManufacturer').val();
//    var orderStatus = 0;//Pending // $('#HSRPOrder_OrderStatus').val();
//    var vehicleStage = $('#HSRPOrder_VehicleStage').val();
//    var orderType = parseInt($('#HSRPOrder_OrderType').val());
//    var fuelType = $('#HSRPOrder_FuelType').val();
//    var vehicleTypes = $('#HSRPOrder_VehicleTypes').val();
//    var vehicleClass = $('#HSRPOrder_VehicleClass').val();
//    //console.log("Order Status:", orderStatus);
//    console.log("Vehicle Stage:", vehicleStage);
//    //console.log("Order Type:", orderType);
//    console.log("Fuel Type:", fuelType);
//    console.log("Vehicle Types:", vehicleTypes);
//    console.log("Vehicle Class:", vehicleClass);
//    //var orderStatus = getOrderStatusEnumValue($('#HSRPOrder_OrderStatus').val());
//    //var vehicleStage = getVehicleStageEnumValue($('#HSRPOrder_VehicleStage').val());
//    //var orderType = getOrderTypeEnumValue($('#HSRPOrder_OrderType').val());
//    //var fuelType = getFuelTypeEnumValue($('#HSRPOrder_FuelType').val());
//    //var vehicleTypes = getVehicleCategoryEnumValue($('#HSRPOrder_VehicleTypes').val());
//    //var vehicleClass = getVehicleClassEnumValue($('#HSRPOrder_VehicleClass').val());
//    var stateCode = $('#HSRPOrder_StateCodeFromNic').val();
//    var ismyHsrpOrder = true;
//    var isPrepaidOrder = false;
//    var isVahanIntegrationCompleted = false;
//    var orderNo = "";
//    var hSRPOrder = {
//        DealerId: $('#HSRPOrder_DealerId').val(),
//        EmbosingStationId: $('#HSRPOrder_EmbosingStationId').val(),
//        RegDate: regdate,//$('#HSRPOrder_RegDate').val(),
//        //RegDate: regdate,
//        VehicleTypes: parseInt(vehicleTypes),
//        VehicleClass: parseInt(vehicleClass),
//        OrderNo: orderNo,
//        OrderType: parseInt(orderType),
//        OrderDate: orderDate,
//        RegistrationNumber: $('#HSRPOrder_RegistrationNumber').val(),
//        ChasisNumber: $('#HSRPOrder_ChasisNumber').val(),
//        EngineNumber: $('#HSRPOrder_EngineNumber').val(),
//        DateofManufacturer: mandate,
//        FuelType: parseInt(fuelType),
//        VehicleStage: parseInt(vehicleStage),
//        isMyHsrpOrder: ismyHsrpOrder,
//        OrderStatus: parseInt(orderStatus),
//        EntryDate: entrydate,
//        IsPrepaidOrder: isPrepaidOrder,
//        EmbossingIssueDate: embossingissueDate,
//        HotFoilingDate: hotFoilingDate,
//        DeliveryNoteId: deliveryNoteId,
//        /*InvoiceGenerateDate: invoiceGenerateDate,*/
//        InvoiceGenerateDate: invoiceGenerateDate,
//        IsVahanIntegrationCompleted: isVahanIntegrationCompleted,
//        StateCodeFromNic: stateCode
//    }
//    var haspaid = true;
//    //var haspaid = //$('#VehicleOwnerInfo_hasPaid').val();
//    //if (haspaid == undefined || haspaid == null)
//    //    haspaid = false;
//    var gst = $('#VehicleOwnerInfo_Gst').val();
//    var paymentAmount = $('#VehicleOwnerInfo_PaymentAmount').val();
//    var totalAmount = $('#VehicleOwnerInfo_TotalAmount').val();
//    var pincode = $('#VehicleOwnerInfo_Pincode').val();
//    var state = $('#VehicleOwnerInfo_State').val();

//    var vehicleOwnerInfo = {
//        Ownername: $('#VehicleOwnerInfo_Ownername').val(),
//        Email: $('#VehicleOwnerInfo_Email').val(),
//        PhoneNumber: $('#VehicleOwnerInfo_PhoneNumber').val(),
//        Address: $('#VehicleOwnerInfo_Address').val(),
//        DoorNo: $('#VehicleOwnerInfo_DoorNo').val(),
//        Pincode: $('#VehicleOwnerInfo_Pincode').val(),
//        //State: $('#VehicleOwnerInfo_State'),
//        District: $('#VehicleOwnerInfo_District').val(),
//        StreetName: $('#VehicleOwnerInfo_StreetName').val(),
//        hasPaid: haspaid,
//        PaymentAmount: parseFloat(paymentAmount),
//        Gst: parseInt(gst),
//        TotalAmount: parseFloat(totalAmount),
//        OrderId: orderid,
//        LocationAddress: stateCode,//$('#VehicleOwnerInfo_LocationAddress').val(),
//        //Pincode: pincode,
//        State: parseInt(state),
//        hasSticker: isSticker,
//        HSRPOrderType: parseInt($('#HSRPOrder_OrderType').val())//5
//    }
//    var paymentGateway = {
//        PaymentId: paymentData.paymentId || '',
//        BankRRn: paymentData.bankRrn || '',
//        OrderId: paymentData.orderId || '',
//        PaymentMethod: paymentData.method || '',
//        PaymentDetails: (paymentData.upi.payer_account_type + ', ' + paymentData.upi.vpa) || '',
//        TotalAmount: parseFloat(paymentData.amount) || 0,
//        ContactNo: paymentData.contact || '',
//        EmailAddress: paymentData.email || ''
//    }
//    var model = {
//        HSRPAppointment: hSRPAppointment,
//        HSRPOrder: hSRPOrder,
//        VehicleOwnerInfo: vehicleOwnerInfo,
//        HSRPPaymentGateway: paymentGateway
//    }
//    console.log("order", model);
//    $.ajax({
//        type: "POST",
//        url: '/Home/BookingDetails',
//        contentType: "application/json",
//        data: JSON.stringify(model),
//        cache: false,
//        success: function (response) {
//            if (response.success) {
//                $('#form7').hide();
//                $('#form8').show();
//                $('#downloadReceiptbtn').attr('href', '/Home/Receipt?orderId=' + response.orderId);
//                $('#downloadReceiptbtn').trigger('click');
//            } else {
//                toastr.error('Failed to place the order. Please try again.', "Error");
//            }
//        },
//        error: function (xhr, status, error) {
//            toastr.error("Failed to place the order. Please try again.", "Error");
//        }
//    });
//}
