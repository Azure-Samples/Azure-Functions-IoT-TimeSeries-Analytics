module.exports = function (context, req) {
    
    context.log(req.body);
    var body = JSON.stringify(req.body);
    var message = {
        "personalizations": [{ "to": [{"email": "YOURE_E_MAIL_HERE"}]}],
        from: { email: "iot@alert.com" },
        subject: "[IoT] Temperature Alert",
        content: [{
            type: 'text/plain',
            value: body
        }]
    };
    context.bindings.message = message;
    context.done();
};