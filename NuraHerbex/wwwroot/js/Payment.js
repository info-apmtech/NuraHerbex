function razorpayIntegrationPaymentConfirm(hasSticker) {
    var paymentAmount = $('#VehicleOwnerInfo_TotalAmount').val();
    var ownerName = $('#VehicleOwnerInfo_Ownername').val();
    var email = $('#VehicleOwnerInfo_Email').val();
    var phoneNumber = $('#VehicleOwnerInfo_PhoneNumber').val();
    var address = $('#VehicleOwnerInfo_Address').val();
    var createdAt = new Date();
    var domain = "BNHSRP";

    // Step 1: Create the order in the other project's backend API
    fetch('https://payment.tracole.com/payment/create-order', {
        //fetch('https://localhost:7106/payment/create-order', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ amount: paymentAmount, domainName: domain })//"BNHSRP" })  // Send the amount for order creation
    })
        .then(response => response.json())
        .then(order => {
            console.log('Order created in another project:', order);  // Log order details for debugging

            if (order && order.id) {
                // Step 2: Now that we have the order_id, we call Razorpay's frontend API
                var options = {
                    key: "rzp_live_UBscLASQP7fAT4",// Tracole Production Razorpay Key
                    //key: "rzp_live_P7elNxhAkthQ7w",// Production Razorpay Key
                    //key: "rzp_test_jSPTP0C4WotdSJ",  // Test Razorpay key
                    amount: paymentAmount * 100, // Convert to paise (Razorpay expects amount in paise)
                    currency: "INR",  // Currency type
                    name: "Tracole Technologies",
                    description: "Order Payment",
                    image: "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSif0XXHotoAg5YT0ltoVrNIfqwKrb5KLWwOo0ITuphqh7qrwnEdVe-aecCnV6Ai8l0awI&usqp=CAU",
                    order_id: order.id,  // Use the order id received from your backend
                    handler: function (response) {
                        console.log('razorpay', response);
                        var paymentData = {
                            paymentId: response.razorpay_payment_id,
                            orderId: response.razorpay_order_id,
                            signature: response.razorpay_signature,
                            domainName: domain,
                            amount: paymentAmount
                        };

                        // Step 3: Verify the payment with your other project's backend API
                        fetch('https://payment.tracole.com/payment/verify-payment', {
                            //fetch('https://localhost:7106/payment/verify-payment', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(paymentData)
                        })
                            .then(response => response.json())
                            .then(verificationResult => {
                                console.log('Payment data', paymentData);
                                console.log('Payment verification result:', verificationResult);
                                if (verificationResult.message === "Payment verified successfully") {
                                    document.getElementById('VehicleOwnerInfo_hasPaid').value = "true";
                                    toastr.success("Payment Successful!", "Success");
                                    fetchPaymentDetails(paymentData.paymentId, hasSticker);
                                    //fetch('https://localhost:7106/payment/fetch-payment', {
                                    //    method: 'POST',
                                    //    headers: { 'Content-Type': 'application/json' },
                                    //    body: JSON.stringify({ paymentId: paymentData.paymentId })
                                    //})
                                    //    .then(response => response.json())
                                    //    .then(fetchedPaymentDetails => {
                                    //        console.log('Fetched payment details:', fetchedPaymentDetails);
                                    //        if (hasSticker === 'true') {
                                    //            paymentConfirmSticker(fetchedPaymentDetails);
                                    //        } else {
                                    //            paymentConfirmSuccess(fetchedPaymentDetails);
                                    //        }
                                    //    })
                                    //    .catch(error => {
                                    //        console.error('Error fetching payment details:', error);
                                    //        alert("Error fetching payment details.");
                                    //    });
                                } else {
                                    document.getElementById('VehicleOwnerInfo_hasPaid').value = "false";
                                    toastr.error("Payment verification failed.", "Error");
                                }
                            })
                            .catch(error => {
                                console.error('Error verifying payment:', error);
                                toastr.error("Error during payment verification.", "Error");
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

                // Step 4: Create the Razorpay payment modal
                var rzp = new Razorpay(options);
                rzp.open();
            } else {
                toastr.error("Error creating order in another project.", "Error");
            }
        })
        .catch(error => {
            console.error('Error creating order in the other project:', error);
            toastr.error("Error creating payment order.", "Error");
        });
}
function fetchPaymentDetails(paymentId, hasSticker) {
    fetch(`https://payment.tracole.com/payment/fetch-payment/${paymentId}`, {
        //fetch(`https://localhost:7106/payment/fetch-payment/${paymentId}`, {
        method: "GET",
        headers: {
            "Accept": "application/json"
        }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            return response.json();
        })
        .then(fetchedPaymentDetails => {
            console.log("Received Payment Details:", fetchedPaymentDetails);

            if (!fetchedPaymentDetails || Object.keys(fetchedPaymentDetails).length === 0) {
                toastr.error("Received empty payment details!", "Error");
                return;
            }

            if (hasSticker === 'true') {
                paymentConfirmSticker(fetchedPaymentDetails);
            } else {
                paymentConfirmSuccess(fetchedPaymentDetails);
            }
        })
        .catch(error => {
            console.error("Error fetching payment details:", error);
            toastr.error("Error fetching payment details.", "Error");
        });
}

function paymentConfirmSuccess(paymentData) {
    var paymentDetails = JSON.stringify(paymentData);
    console.log('fetchpayment', paymentDetails);
    var orderid = 0;
    var appointmentType = $('#HSRPAppointment_AppointmentType').val();
    console.log('apptype', appointmentType);
    var createdAt = new Date();
    var isSticker = false;

    $('#HSRPOrder_CreatedAt').val(createdAt);
    var hSRPAppointment = {
        AppointmentId: parseInt($('#HSRPAppointment_AppointmentId').val()),
        OrderId: orderid,
        AppointmentType: getAppointmentTypeEnumValue(appointmentType),
        CreatedAt: createdAt,
        hasSticker: isSticker
    }
    console.log('hsrpapp', hSRPAppointment);
    var deliveryNoteId = 0;
    var embossingissueDate = new Date('0001-01-01');
    console.log('embdate', embossingissueDate);
    var entrydate = new Date();
    var hotFoilingDate = new Date('0001-01-01');
    var invoiceGenerateDate = new Date('0001-01-01');
    var orderDate = new Date();
    var regdate = $('#HSRPOrder_RegDate').val();
    var mandate = $('#HSRPOrder_DateofManufacturer').val();
    var orderStatus = 0;//Pending // $('#HSRPOrder_OrderStatus').val();
    var vehicleStage = $('#HSRPOrder_VehicleStage').val();
    var orderType = parseInt($('#HSRPOrder_OrderType').val());
    var fuelType = $('#HSRPOrder_FuelType').val();
    var vehicleTypes = $('#HSRPOrder_VehicleTypes').val();
    var vehicleClass = $('#HSRPOrder_VehicleClass').val();
    //console.log("Order Status:", orderStatus);
    console.log("Vehicle Stage:", vehicleStage);
    //console.log("Order Type:", orderType);
    console.log("Fuel Type:", fuelType);
    console.log("Vehicle Types:", vehicleTypes);
    console.log("Vehicle Class:", vehicleClass);
    //var orderStatus = getOrderStatusEnumValue($('#HSRPOrder_OrderStatus').val());
    //var vehicleStage = getVehicleStageEnumValue($('#HSRPOrder_VehicleStage').val());
    //var orderType = getOrderTypeEnumValue($('#HSRPOrder_OrderType').val());
    //var fuelType = getFuelTypeEnumValue($('#HSRPOrder_FuelType').val());
    //var vehicleTypes = getVehicleCategoryEnumValue($('#HSRPOrder_VehicleTypes').val());
    //var vehicleClass = getVehicleClassEnumValue($('#HSRPOrder_VehicleClass').val());
    var stateCode = $('#HSRPOrder_StateCodeFromNic').val();
    var ismyHsrpOrder = true;
    var isPrepaidOrder = false;
    var isVahanIntegrationCompleted = false;
    var orderNo = "";
    var hSRPOrder = {
        DealerId: $('#HSRPOrder_DealerId').val(),
        EmbosingStationId: $('#HSRPOrder_EmbosingStationId').val(),
        RegDate: regdate,//$('#HSRPOrder_RegDate').val(),
        //RegDate: regdate,
        VehicleTypes: parseInt(vehicleTypes),
        VehicleClass: parseInt(vehicleClass),
        OrderNo: orderNo,
        OrderType: parseInt(orderType),
        OrderDate: orderDate,
        RegistrationNumber: $('#HSRPOrder_RegistrationNumber').val(),
        ChasisNumber: $('#HSRPOrder_ChasisNumber').val(),
        EngineNumber: $('#HSRPOrder_EngineNumber').val(),
        DateofManufacturer: mandate,
        FuelType: parseInt(fuelType),
        VehicleStage: parseInt(vehicleStage),
        isMyHsrpOrder: ismyHsrpOrder,
        OrderStatus: parseInt(orderStatus),
        EntryDate: entrydate,
        IsPrepaidOrder: isPrepaidOrder,
        EmbossingIssueDate: embossingissueDate,
        HotFoilingDate: hotFoilingDate,
        DeliveryNoteId: deliveryNoteId,
        /*InvoiceGenerateDate: invoiceGenerateDate,*/
        InvoiceGenerateDate: invoiceGenerateDate,
        IsVahanIntegrationCompleted: isVahanIntegrationCompleted,
        StateCodeFromNic: stateCode
    }
    var haspaid = true;
    //var haspaid = //$('#VehicleOwnerInfo_hasPaid').val();
    //if (haspaid == undefined || haspaid == null)
    //    haspaid = false;
    var gst = $('#VehicleOwnerInfo_Gst').val();
    var paymentAmount = $('#VehicleOwnerInfo_PaymentAmount').val();
    var totalAmount = $('#VehicleOwnerInfo_TotalAmount').val();
    var pincode = $('#VehicleOwnerInfo_Pincode').val();
    var state = $('#VehicleOwnerInfo_State').val();

    var vehicleOwnerInfo = {
        Ownername: $('#VehicleOwnerInfo_Ownername').val(),
        Email: $('#VehicleOwnerInfo_Email').val(),
        PhoneNumber: $('#VehicleOwnerInfo_PhoneNumber').val(),
        Address: $('#VehicleOwnerInfo_Address').val(),
        DoorNo: $('#VehicleOwnerInfo_DoorNo').val(),
        Pincode: $('#VehicleOwnerInfo_Pincode').val(),
        //State: $('#VehicleOwnerInfo_State'),
        District: $('#VehicleOwnerInfo_District').val(),
        StreetName: $('#VehicleOwnerInfo_StreetName').val(),
        hasPaid: haspaid,
        PaymentAmount: parseFloat(paymentAmount),
        Gst: parseInt(gst),
        TotalAmount: parseFloat(totalAmount),
        OrderId: orderid,
        LocationAddress: stateCode,//$('#VehicleOwnerInfo_LocationAddress').val(),
        //Pincode: pincode,
        State: parseInt(state),
        hasSticker: isSticker,
        HSRPOrderType: parseInt($('#HSRPOrder_OrderType').val())//5
    }
    var paymentGateway = {
        PaymentId: paymentData.paymentId || '',
        BankRRn: paymentData.bankRrn || '',
        OrderId: paymentData.orderId || '',
        PaymentMethod: paymentData.method || '',
        PaymentDetails: (paymentData.upi.payer_account_type + ', ' + paymentData.upi.vpa) || '',
        TotalAmount: parseFloat(paymentData.amount) || 0,
        ContactNo: paymentData.contact || '',
        EmailAddress: paymentData.email || ''
    }
    var model = {
        HSRPAppointment: hSRPAppointment,
        HSRPOrder: hSRPOrder,
        VehicleOwnerInfo: vehicleOwnerInfo,
        HSRPPaymentGateway: paymentGateway
    }
    console.log("order", model);
    $.ajax({
        type: "POST",
        url: '/Home/BookingDetails',
        contentType: "application/json",
        data: JSON.stringify(model),
        cache: false,
        success: function (response) {
            if (response.success) {
                $('#form7').hide();
                $('#form8').show();
                $('#downloadReceiptbtn').attr('href', '/Home/Receipt?orderId=' + response.orderId);
                $('#downloadReceiptbtn').trigger('click');
            } else {
                toastr.error('Failed to place the order. Please try again.', "Error");
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Failed to place the order. Please try again.", "Error");
        }
    });
}
