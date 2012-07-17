function updateLayout() {
    var messengerMode = false;
    if (typeof $('body').data('messengermode') == 'boolean')
        messengerMode = $('body').data('messengermode');

    var aside = messengerMode ? $("#aside_messenger") : $("#aside");

    if (messengerMode) {
        $(".content").css('width', $("#container").width());
        $(".messages").css('height', $(window).height() - $("#header").outerHeight() - $("#footer").outerHeight() - $("#tabs ul").outerHeight() - 14);
        $(".messages").css('width', $("#container").width() - aside.width() - 20);
        $("#messageInput").css('width', $("#footer").outerWidth() - $("#sendButton").outerWidth() - 50);
		$("#aside_messenger").css('height', Math.max($("#tabs:visible").outerHeight() - 0, 280));
    }
    else {
        $(".content").css('width', $("#container").width());
        $(".messages").css('height', $(window).height() - $("#header").outerHeight() - $("#footer").outerHeight() - $("#tabs ul").outerHeight() - 24);
        $(".messages").css('width', $("#container").width() - aside.width() - 23);
        $("#messageInput").css('width', $("#footer").outerWidth() - $("#sendButton").outerWidth() - 50);
        $("#aside").css('height', $("#tabs:visible").outerHeight() - 28);
    }

    $("#tabs").css('width', $("#container").width() - aside.width());
}