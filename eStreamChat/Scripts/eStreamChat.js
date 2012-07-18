var chatRoomId = -1; // The current chat room id
var activePanel; // The jquery object of the currently active tab
var eventsTimer; // The timer that check for new chat events
var userId; // Current user id
var token; // Authorization token
var lastTimestamp = 0; // The last received event timestamp
var onlineUsers = new Object(); // Contains the data for the online users; object used as hashtable
var initialEventLoad = true; // A flag that is set to false on the first successful event load
var kicked = false; // A flag that is set to true when the user is kicked or banned.
var isAdmin = false;
var alertEnabled = false;
var videoChatEnabled = false;
var fileTransferEnabled = false;
var flashMediaServer;
var webcamdetected = false;
var currentVideoBroadcastGuid = null;
var messengerMode = false;
var messengerTargetUserId = null;
var messengerIsInitiator = false;
var imUserCanSendMessages = false; // Prevents sending messages while the other IM user has not already joined the room
var broadcastVideoWidth = 640;
var broadcastVideoHeight = 480;
var broadcastVideoWindowWidth = 320;
var broadcastVideoWindowHeight = 240;
var receiveVideoWindowWidth = 320;
var receiveVideoWindowHeight = 240;

$(function () {
    if (typeof $('body').data('messengermode') == 'boolean')
        messengerMode = $('body').data('messengermode');

    if (messengerMode) {
        messengerTargetUserId = $.urlParam('target');
        messengerIsInitiator = $.urlParam('init') == "1";
    }

    var templateUrl = $('#templates').data('url');
    $('#templates').load(templateUrl, function () {
        // Set css class based on the browser
        if ($.browser.msie) {
            $("body").addClass('msie');
            $("body").addClass('msie' + $.browser.version.substr(0, 1));
        }
        if ($.browser.webkit) $("body").addClass('webkit');
        if ($.browser.mozilla) $("body").addClass('mozilla');

        if ($.browser.msie && parseInt($.browser.version, 10) < 9) {
            $("label img").live("click", function () {
                $("#" + $(this).parents("label").attr("for")).focus();
                $("#" + $(this).parents("label").attr("for")).click();
            });
        }

        // Initialize color picker
        $('#textColor').colorPicker();

        $('#checkAlert').click(function () {
            alertEnabled = $('#checkAlert').is(':checked');
        });

        $('#checkVideoBroadcast').click(function () {
            var alreadyOpened;
            if (messengerMode)
                alreadyOpened = !$('#divCurrentUserVideo').is(':empty');
            else
                alreadyOpened = $('#divBroadcastVideo').length != 0;
            broadcastVideo(!alreadyOpened);
        });

        $('#fileUploadDialogButton').bind('click', function () {
            $('#fileUploadDialog').dialog(
                {
                    close: function (ev, ui) {
                        focusMessageField();
                    }
                });
        });

        $("#fileUploadDialog #uploadButton").bind('click', function () {
            var toUserId = getTargetUserId();
            var sendFileURL = 'SendFile.ashx?token=' + token + "&chatRoomId=" + chatRoomId;
            if (toUserId != null)
                sendFileURL += "&toUserId=" + toUserId;

            $.ajaxFileUpload(
                {
                    url: sendFileURL,
                    secureuri: false,
                    fileElementId: 'fileUpload',
                    dataType: 'json',
                    success: function (data, status) {
                        $('#fileUploadDialog').dialog('close');
                        if (typeof (data.error) != 'undefined') {
                            if (data.error != '') {
                                alert(data.error);
                            } else {
                                var messagePanel = getPanelByUserId(toUserId);
                                messagePanel.append($('#fileSentTemplate').jqote());
                            }
                        }
                    },
                    error: function (data, status, e) {
                        alert(e);
                    }
                }
            );
        });

        // Set default jqote tag
        $.jqotetag('*');

        // Initialize jquery ui tabs & buttons
        $("#tabs").tabs({
            tabTemplate: '<li><a href="#{href}">#{label}</a> <span class="ui-icon ui-icon-close" style="cursor:pointer">Close Tab</span></li>',
            add: function (event, ui) {
                $('#' + ui.panel.id).addClass('messages');
                updateLayout();

                $(ui.tab).next('.ui-icon-close').attr('id', "close-" + ui.panel.id);
                $('#close-' + ui.panel.id).bind('click', function () {
                    var index = $('li', $("#tabs")).index($(this).parent());
                    $(this).parent().hide();
                    $("#tabs").tabs('remove', index);
                });
            },
            select: function (event, ui) {
                activePanel = $('#' + ui.panel.id);
                activePanel.parent().stopBlink();
            },
            show: function (event, ui) {
                activePanel = $('#' + ui.panel.id);
                scrollToBottom();
                focusMessageField();
            }
        });

        if (messengerMode) {
            $('#tabs ul li').hide();
        }

        // Set default active tab
        if (!messengerMode)
            activePanel = $('#panel-room');
        else {
            $('#panel-room').attr('id', 'panel-' + messengerTargetUserId);
            activePanel = $('#panel-' + messengerTargetUserId);
        }

        // Prepare the text formatting buttons
        $("#formatButtons").buttonset();

        $('#checkAlert').button();
        $('#checkVideoBroadcast').button();

        //gives focus back to the message input field when a button is clicked
        $("#button-panel input, .color_picker_wrap, #color_selector, #button-panel select>option").each(function () { $(this).click(function () { focusMessageField(); }); });
        $("#button-panel select").each(function () {
            $(this).blur(function () { focusMessageField(); });
            $(this).change(function () { focusMessageField(); });
        });
        $('#button-panel select>option').each(function () { $(this).mousedown(function () { focusMessageField(); }); });

        // Update the layout
        updateLayout();
        $(window).resize(function () {
            updateLayout();
        });

        // Attach the send button handlers
        $("#sendButton").click(function () {
            sendMessage();
            return false;
        });

        // Catch the enter button on the new message textbox
        $("#messageInput").keypress(function (event) {
            if (event.keyCode == 13) {
                event.preventDefault();
                sendMessage();
                return false;
            }
        });

        // Get chat room id from parameter
        if (!messengerMode) {
            if ($.urlParam('roomId') != null)
                chatRoomId = $.urlParam('roomId');
        } else {
            //messenger room id is -2. All conversation inside the room should be private
            chatRoomId = -2;
        }

        // Configure alert popup for errors
        $.ajaxSetup({
            error: function (req, status, error) {
                alert(status + ' - ' + req.responseText);
            }
        });

        // Try to join the chat room
        $.ajax({
            type: "POST",
            url: "ChatEngine.svc/JoinChatRoom",
            data: '{"chatRoomId":"' + chatRoomId + '", "href":"' + window.location.href.replace(/'/g, "\'").replace(/\\/g, "\\\\") + '"}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                joinChatRoomSuccess(msg.d);
            }
        });
    });
});

function focusMessageField() {
    var msgField = $('#messageInput');
    var length = msgField.val().length;
    msgField.focus();
    msgField.setCursorPosition(length, length);
}

function playSound(url) {
    var browser = navigator.appName;
    if (browser == "Microsoft Internet Explorer") {
        document.all.sound.src = url;
    } else {
        $("#spanSound").html('<embed src="' + url + '" hidden="true" autostart="true" loop="false">');
    }
}

function getPrivateTabIndex(userId) {
    var index = $('li', $("#tabs")).index($('#close-panel-' + userId).parent());
    return index;
}

function joinChatRoomSuccess(result) {
    if (result.Error != null) {
        $.alert("Unable to join room!", result.Error);

        // Redirect to login url if requested
        if (result.RedirectUrl != null) {
            window.location.href = result.RedirectUrl;
        }
        return;
    }

    // Save user token
    token = result.Token;
    userId = result.UserId;
    isAdmin = result.IsAdmin;
    fileTransferEnabled = result.FileTransferEnabled;
    videoChatEnabled = result.VideoChatEnabled;
    flashMediaServer = result.FlashMediaServer;

    if (videoChatEnabled)
        $('#webcamdetector').append($('#webcamDetectorTemplate').jqote());

    (!videoChatEnabled || !webcamdetected) ? $("#videoBroadcastButtonContainer").hide() : $("#videoBroadcastButtonContainer").show();
    (!fileTransferEnabled) ? $("#fileUploadDialogButton").hide() : $("#fileUploadDialogButton").show();


    startEventsTimer();

    // Set window unload events
    $(window).bind('beforeunload', function() {
        return 'Do you really want to exit the chat?';
    });
    $(window).bind('unload', function() {
        if (!kicked) {
            $.ajax({
                type: "POST",
                url: "ChatEngine.svc/LeaveChatRoom",
                data: '{"chatRoomId":"' + chatRoomId + '", "token":"' + token + '", "messengerTargetUserId":'
                + (messengerMode ? '"' + messengerTargetUserId + '"' : 'null') + '}',
                contentType: "application/json; charset=utf-8",
                dataType: "json"
            });
        }
    });

    // Save the online users data
    for (var i = 0; i < result.Users.length; i++) {
        var user = result.Users[i];
        onlineUsers[user.Id] = user;
    }

    // Save broadcasts
    for (var i = 0; i < result.Broadcasts.length; i++) {
        if (onlineUsers[result.Broadcasts[i].Key] != undefined) {
            onlineUsers[result.Broadcasts[i].Key].Guid = result.Broadcasts[i].Value;
        }
    }

    // Prepare the online users list
    updateOnlineUsers();

    if (!messengerMode) {
        // Set chat room name to first tab
        $('#tabs ul li:first a').text(result.ChatRoomName);

        // Print initial messages
        outputSystemMessage("Connected!");
        outputSystemMessage(result.ChatRoomTopic);
    } else {
        if (messengerIsInitiator && location.hash != 'connected' /*onlineUsers[messengerTargetUserId] == undefined*/) {
            outputSystemMessage("Awaiting other user to accept the chat request...");
        } else {
            outputSystemMessage("Connected!");
            imUserCanSendMessages = true;
            location.hash = 'connected';
        }
    }
}

function broadcastVideo(enable) {
    if (enable) {
        var targetUserId = getTargetUserId();
        $.ajax({
            type: "POST",
            url: "ChatEngine.svc/BroadcastVideo",
            data: '{"prevGuid":' + (currentVideoBroadcastGuid == null ? 'null' : '"' + currentVideoBroadcastGuid + '"') +
            ', "token":"' + token + '", "chatRoomId":"' + chatRoomId +
            '", "targetUserId":' + (targetUserId == null ? 'null' : '"' + targetUserId + '"') + '}',
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function(result) {
                currentVideoBroadcastGuid = result.d;
                var alreadyOpened;

                if (messengerMode) {
                    alreadyOpened = !$('#divCurrentUserVideo').is(':empty');

                    if (!alreadyOpened) {
                        var videoWindow = $('#broadcastVideoWindowTemplate').jqote({ Guid: currentVideoBroadcastGuid, FlashMediaServer: flashMediaServer, BroadcastVideoWidth: broadcastVideoWidth, BroadcastVideoHeight: broadcastVideoHeight });
                        $('#divCurrentUserVideo').append(videoWindow);

                        $('#divCurrentUserVideo').css("background-image", "");
                        $('#divCurrentUserVideo').css("background-color", "black");


                        if (onlineUsers[userId] != undefined)
                            onlineUsers[userId].Guid = currentVideoBroadcastGuid;
                    }
                } else {
                    alreadyOpened = $('#divBroadcastVideo').length != 0;

                    if (!alreadyOpened) {
                        var videoWindow = $('#broadcastVideoWindowTemplate').jqote({ Guid: currentVideoBroadcastGuid, FlashMediaServer: flashMediaServer, BroadcastVideoWidth: broadcastVideoWidth, BroadcastVideoHeight: broadcastVideoHeight });
                        $('#videosPlaceholder').append(videoWindow);

                        $('#divBroadcastVideo').dialog({
                            modal: false,
                            autoOpen: true,
                            height: (broadcastVideoWindowHeight),
                            width: (broadcastVideoWindowWidth),
                            draggable: true,
                            resizable: true,
                            closeOnEscape: false,
                            close: function(ev, ui) {
                                broadcastVideo(false);
                                focusMessageField();
                            }
                        });

                        if (onlineUsers[userId] != undefined)
                            onlineUsers[userId].Guid = currentVideoBroadcastGuid;

                        $('#divBroadcastVideo').css({ 'top': '40px', 'left': '350px' });
                        var webcamIcon = $('#webcam' + userId);
                        webcamIcon.unbind('click');
                        webcamIcon.show();
                    }
                }

            }
        });
    } else {
        var alreadyOpened;

        if (messengerMode) {
            alreadyOpened = !$('#divCurrentUserVideo').is(':empty');
            $('#divCurrentUserVideo').empty();
            $('#divCurrentUserVideo').css("background-image", "url(" + onlineUsers[userId].PhotoUrl + ")");
            $('#divCurrentUserVideo').css("background-color", "#ccc");
        } else {
            alreadyOpened = $('#divBroadcastVideo').length != 0;

            if (alreadyOpened) {
                $('#divBroadcastVideo').dialog('destroy');
                $('#divBroadcastVideo').remove();
            }
        }

        if (alreadyOpened && currentVideoBroadcastGuid != null) {
            $.ajax({
                type: "POST",
                url: "ChatEngine.svc/StopVideoBroadcast",
                data: '{"token":"' + token + '", "chatRoomId":"' + chatRoomId + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json"
            });

            var webcamIcon = $('#webcam' + userId);
            webcamIcon.unbind('click');
            webcamIcon.hide();

            if (onlineUsers[userId] != undefined)
                delete onlineUsers[userId].Guid;
        }

        currentVideoBroadcastGuid = null;
    }
}

function receiveVideo(senderUserId, guid) {
    if (messengerMode) {
        var alreadyOpened = !$('#divTargetUserVideo').is(':empty');

        if (!alreadyOpened) {
            var videoWindow = $('#receiveVideoWindowTemplate').jqote({ SenderUserId: senderUserId, Guid: guid, FlashMediaServer: flashMediaServer });
            $('#divTargetUserVideo').append(videoWindow);

            $('#divTargetUserVideo').css("background-image", "");
            $('#divTargetUserVideo').css("background-color", "black");
        }
    } else {
        var alreadyOpened = $('#divReceiveVideo' + senderUserId).length != 0;

        if (!alreadyOpened) {
            var videoWindow = $('#receiveVideoWindowTemplate').jqote({ SenderUserId: senderUserId, Guid: guid, FlashMediaServer: flashMediaServer });
            $('#videosPlaceholder').append(videoWindow);
            $('#divReceiveVideo' + senderUserId).dialog({
                modal: false,
                autoOpen: true,
                height: receiveVideoWindowHeight,
                width: receiveVideoWindowWidth,
                draggable: true,
                resizable: true,
                closeOnEscape: false,
                close: function(ev, ui) {
                    closeVideoReceiver(senderUserId);
                    focusMessageField();
                }
            });
            $('#divReceiveVideo' + senderUserId).css({ 'top': '40px', 'left': '550px' });
        }
    }
}

function closeVideoReceiver(senderUserId) {
    if (messengerMode) {
        if (onlineUsers[messengerTargetUserId] != undefined) {
            $('#divTargetUserVideo').empty();
            $('#divTargetUserVideo').css("background-image", "url(" + onlineUsers[messengerTargetUserId].PhotoUrl + ")");
            $('#divTargetUserVideo').css("background-color", "#ccc");
        }
    } else {
        $('#divReceiveVideo' + senderUserId).dialog('destroy');
        $('#divReceiveVideo' + senderUserId).remove();
    }
}

function updateOnlineUsers() {
    if (messengerMode) {
        if (onlineUsers[userId] != undefined && onlineUsers[userId].Guid == undefined)
            $('#divCurrentUserVideo').css("background-image", "url(" + onlineUsers[userId].PhotoUrl + ")");
        if (onlineUsers[messengerTargetUserId] != undefined)
            $('#divTargetUserVideo').css("background-image", "url(" + onlineUsers[messengerTargetUserId].PhotoUrl + ")");
        return;
    }

    // Sort users
    var tempArray = new Array();
    i = 0;
    for (var key in onlineUsers) {
        tempArray[i] = onlineUsers[key].DisplayName + '||' + key;
        i++;
    }
    tempArray = tempArray.sort();
    var sortedUsers = new Array();
    for (i = 0; i < tempArray.length; i++) {
        var temp = tempArray[i].split('||');
        sortedUsers.push(onlineUsers[temp[1]]);
    }

    $('#onlineUsers').jqotesub('#onlineUserTemplate', sortedUsers);
    $("#onlineUsers .context-menu-target").contextMenu({
            menu: isAdmin ? 'adminMenu' : 'userMenu'
        }, function(action, el, pos) {
            var targetUserId = $(el).data("userId");
            var targetUserDisplayName = $(el).text();

            if (action == "private_chat") {
                $(el).click();
            } else if (action == "ignore_user") {
                $.ajax({
                    type: "POST",
                    url: "ChatEngine.svc/SendCommand",
                    data: '{"chatRoomId":"' + chatRoomId + '", "token":"' + token + '", "targetUserId":"' + targetUserId + '", "command":"ignore"}',
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function(msg) {
                        outputSystemMessage("You have added " + targetUserDisplayName + " to your ignore list!", true);
                    }
                });
            } else if (action == "kick_user") {
                $.ajax({
                    type: "POST",
                    url: "ChatEngine.svc/SendCommand",
                    data: '{"chatRoomId":"' + chatRoomId + '", "token":"' + token + '", "targetUserId":"' + targetUserId + '", "command":"kick"}',
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function(msg) {
                    }
                });
            } else if (action == "ban_user") {
                $.ajax({
                    type: "POST",
                    url: "ChatEngine.svc/SendCommand",
                    data: '{"chatRoomId":"' + chatRoomId + '", "token":"' + token + '", "targetUserId":"' + targetUserId + '", "command":"ban"}',
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function(msg) {
                    }
                });
            } else if (action == "slap_user") {
                $.ajax({
                    type: "POST",
                    url: "ChatEngine.svc/SendCommand",
                    data: '{"chatRoomId":"' + chatRoomId + '", "token":"' + token + '", "targetUserId":"' + targetUserId + '", "command":"slap"}',
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function(msg) {
                        outputSystemMessage("You slap " + targetUserDisplayName + " around with a large trout.", true);
                    }
                });
            } else {
                //the item is hyperlink (action == url) so we want to open it up
                //returning true will activate the hyperlink
                return true;
            }

            return false;
        }, //onshow menu
        function(jqSrcElement, jqMenu) {
            var userId = jqSrcElement.data("userId");
            var user = onlineUsers[userId];
            var a = jqMenu.find('LI.view-profile>A');
            if (!(user && user.ProfileUrl && user.ProfileUrl != "#")) {
                jqMenu.disableContextMenuItemsByClassName('view-profile');
                a.attr('href', 'javascript:void(0)');
                a.removeAttr('target');
            } else {
                jqMenu.enableContextMenuItemsByClassName('view-profile');
                a.attr('href', user.ProfileUrl);
                a.attr('target', '_blank');
            }
        });
}

function startEventsTimer() {
    eventsTimer = setInterval("getEvents()", 5000);
    getEvents();
}

function stopEventsTimer() {
    clearInterval(eventsTimer);
}

function getEvents() {
    // Try to join the chat room
    $.ajax({
        type: "POST",
        url: "ChatEngine.svc/GetEvents",
        data: '{"chatRoomId":"' + chatRoomId + '", "token":"' + token + '", "fromTimestamp":"' + lastTimestamp + '", "messengerTargetUserId":'
        + (messengerMode ? '"' + messengerTargetUserId + '"' : 'null') + '}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(msg) {
            getEventsSuccess(msg.d);
        }
    });
}

function getEventsSuccess(result) {
    if (result.Error != null) {
        $.alert("Error!", result.Error);
        return;
    }
    for (var i = 0; i < result.Messages.length; i++) {
        switch (result.Messages[i].MessageType) {
        case 1: // System message
            outputSystemMessage(result.Messages[i].Content);
            break;
        case 2: // User message
            if (initialEventLoad || result.Messages[i].FromUserId != userId) {
                outputUserMessage(result.Messages[i]);

                if (alertEnabled) {
                    if (isPrivateMessage(result.Messages[i]))
                        playSound('..\\PrivateMessage.wav');
                    else
                        playSound('..\\PublicMessage.wav');
                }
            }
            break;
        case 4: // User joined
            if (!initialEventLoad)
                outputSystemMessage(result.Messages[i].Content);
            break;
        case 5: // User left
            if (!initialEventLoad)
                outputSystemMessage(result.Messages[i].Content);
            break;
        case 6: // Send file
        case 7: // Send image file
            outputUserMessage(result.Messages[i]);
            break;
        case 8: // User kicked or banned
            outputSystemMessage(result.Messages[i].Content);

            if (result.Messages[i].FromUserId == userId)
                closeChat();

            kicked = true;
            break;
        case 9: //video broadcast
            outputUserMessage(result.Messages[i]);
            break;
        case 10:
            if (!initialEventLoad) {
                if (onlineUsers[result.Messages[i].FromUserId] != undefined)
                    delete onlineUsers[result.Messages[i].FromUserId].Guid;
                $('#webcam' + result.Messages[i].FromUserId).hide();

                var alreadyOpened = $('#divReceiveVideo' + result.Messages[i].FromUserId).length != 0;
                if (alreadyOpened) {
                    closeVideoReceiver(result.Messages[i].FromUserId);
                }
            }
            break;
        case 12: //RequestAccepted = 12
            location.hash = 'connected';
            imUserCanSendMessages = true;
            break;
        case 13: //RequestDeclined = 13
            outputSystemMessage(result.Messages[i].Content);
            break;
        default:
            break;
        }

        if (result.Messages[i].Timestamp > lastTimestamp)
            lastTimestamp = result.Messages[i].Timestamp;
    }

    if (!initialEventLoad) {
        for (var i = 0; i < result.UsersJoined.length; i++) {
            var user = result.UsersJoined[i];
            onlineUsers[user.Id] = user;
            updateOnlineUsers();
        }
        for (var i = 0; i < result.UsersLeft.length; i++) {
            var user = result.UsersLeft[i];
            delete onlineUsers[user.Id];
            updateOnlineUsers();
        }
    }

    initialEventLoad = false;

    if (result.CallInterval != null) {
        clearInterval(eventsTimer);
        eventsTimer = setInterval("getEvents()", result.CallInterval);
    }
}

function closeChat() {
    stopEventsTimer();
    disableInterface();
}

function disableInterface() {
    $('body').append('<div id="fade" style="z-index:99999999;background: #000;position: fixed; left: 0; top: 0;width: 100%; height: 100%;opacity: .60;"></div>');
    $('#fade').css({ 'filter': 'alpha(opacity=60)' }).fadeIn();
}

function outputSystemMessage(message, showInCurrentTab) {
    var panelId = showInCurrentTab == true || messengerMode ? activePanel.attr('id') : "panel-room";
    $('#' + panelId).append(
        $('#systemMessageTemplate').jqote({ Message: message })
    );

    scrollToBottom();
}

function isPrivateMessage(message) {
    return message.ToUserId != null;
}

function isPanelActive(messagePanel) {
    return messagePanel.attr('id') == activePanel.attr('id');
}

function formatOptionsToString(formatOptions) {
    var formatting = '';
    if (formatOptions != undefined) {
        if (formatOptions.Bold)
            formatting += "font-weight: bold;";
        if (formatOptions.Italic)
            formatting += "font-style: italic;";
        if (formatOptions.Underline)
            formatting += "text-decoration: underline;";
        if (formatOptions.Color != undefined && formatOptions.Color != '')
            formatting += "color: " + formatOptions.Color + ";";
        if (formatOptions.FontName != undefined && formatOptions.FontName != '')
            formatting += "font-family: " + formatOptions.FontName + ";";
        if (formatOptions.FontSize != undefined && formatOptions.FontSize != '')
            formatting += "font-size: " + formatOptions.FontSize + "px;";
    }

    return formatting;
}

function outputUserMessage(message) {
    var user = onlineUsers[message.FromUserId];
    if (user == undefined) return; // User left; do not print messages

    var messagePanel = getPanelForMessage(message);

    if (isPrivateMessage(message) && !isPanelActive(messagePanel)) {
        blinkPanelTab(messagePanel);
    }

    //if text message
    if (message.MessageType == 2) {
        var formatting = formatOptionsToString(message.FormatOptions);

        messagePanel.append(
            $('#userMessageTemplate').jqote({
                ThumbnailUrl: user.ThumbnailUrl,
                DisplayName: user.DisplayName,
                Message: $().emoticon(message.Content),
                FormatOptions: formatting
            }));
    }
    
        //if generic file or image file
    else if (message.MessageType == 6 || message.MessageType == 7) {
        var templateID = message.MessageType == 6 ? "#incomingFileTemplate" : "#incomingImageTemplate";

        if (userId != message.FromUserId) {
            messagePanel.append($(templateID).jqote({ FileUrl: /*$.URLEncode(*/message.Content/*)*/, DisplayName: user.DisplayName }));
        }
    } else if (message.MessageType == 9) {
        if (userId != message.FromUserId) {
            messagePanel.append($('#incomingVideoTemplate').jqote({ DisplayName: user.DisplayName, SenderUserId: message.FromUserId, Guid: message.Content }));

            if (!initialEventLoad) {
                if (onlineUsers[message.FromUserId] != undefined)
                    onlineUsers[message.FromUserId].Guid = message.Content;
                //broadcasts[message.FromUserId] = message.Content;
                var webcamIcon = $('#webcam' + message.FromUserId);
                webcamIcon.unbind('click'/*, OnWebcamClick*/);
                webcamIcon.bind('click', { Guid: message.Content, SenderUserId: message.FromUserId }, OnWebcamClick);
                webcamIcon.show();
            }
        }
    }

    scrollToBottom();
}

function scrollToBottom() {
    activePanel.scrollTop(activePanel.prop("scrollHeight"));
}

function OnWebcamClick(event) {
    receiveVideo(event.data.SenderUserId, event.data.Guid);
}

function blinkPanelTab(messagePanel) {
    $('a[href$="#' + messagePanel.attr('id') + '"]').parent().blink();
}

function sendMessage() {
    if (messengerMode && !imUserCanSendMessages)
        return;

    var message = $('#messageInput').val();
    $('#messageInput').val('');
    if (message == '') return;

    var toUserId = getTargetUserId();

    // Get formatting options
    var isBold = $('#checkBold').is(':checked');
    var isItalic = $('#checkItalic').is(':checked');
    var isUnderline = $('#checkUnderline').is(':checked');
    var color = $('#textColor').val();
    var fontName = $('#dropFontName').val();
    var fontSize = $('#dropFontSize').val();

    // Print the user message instantly
    outputUserMessage({
        MessageType: 2,
        FromUserId: userId,
        Content: $.htmlEncode(message),
        ToUserId: toUserId,
        FormatOptions: { Bold: isBold, Italic: isItalic, Underline: isUnderline, Color: color, FontName: fontName, FontSize: fontSize }
    });

    var ajaxData = '{"chatRoomId":"' + chatRoomId + '", "token":"' + token + '", "message":"' + message.replace( /'/g , "\'").replace( /\\/g , "\\\\") + '"';
    if (toUserId != null) ajaxData += ', "toUserId":"' + toUserId + '"';
    if (isBold) ajaxData += ', "bold":true';
    if (isItalic) ajaxData += ', "italic":true';
    if (isUnderline) ajaxData += ', "underline":true';
    ajaxData += ', "color":"' + color + '"';
    if (fontName != '') ajaxData += ', "fontName":"' + fontName + '"';
    if (fontSize != '') ajaxData += ', "fontSize":"' + fontSize + '"';

    ajaxData += '}';

    // Send message to server
    $.ajax({
        type: "POST",
        url: "ChatEngine.svc/SendMessage",
        data: ajaxData,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function(msg) {
            //$.debug(msg);
        }
    });
}

function privateChat(userId) {
    if (getPrivateTabIndex(userId) == -1) {
        createPrivateChatTab(userId, true);
    } else {
        $("#tabs").tabs('select', getPrivateTabIndex(userId));
    }
}

function getTargetUserId() {
    var targetUserId = null;
    if (messengerMode) {
        targetUserId = messengerTargetUserId;
    } else {
        if (activePanel.attr('id') != 'panel-room') {
            targetUserId = activePanel.attr('id').replace('panel-', '');
        }
    }

    return targetUserId;
}

function createPrivateChatTab(userId, select) {
    var user = onlineUsers[userId];
    $("#tabs").tabs('add', '#panel-' + userId, user.DisplayName);
    if (select == true) {
        $("#tabs").tabs('select', $("#tabs").tabs('length') - 1);
    }
}

function getPanelForMessage(message) {
    if (!messengerMode) {
        var messagePanel = $('#panel-room');
        if (message.ToUserId != null) {
            var panelId = message.FromUserId;
            if (panelId == userId) panelId = message.ToUserId;
            if (getPrivateTabIndex(panelId) == -1) {
                createPrivateChatTab(panelId);
            }
            messagePanel = $('#panel-' + panelId);
        }

        return messagePanel;
    } else {
        return getPanelByUserId(messengerTargetUserId);
    }
}

function getPanelByUserId(userId) {
    if (userId == null)
        return $('#panel-room');
    else
        return $('#panel-' + userId);
}

$.fn.setCursorPosition = function(pos) {
    this.each(function(index, elem) {
        if (elem.setSelectionRange) {
            elem.setSelectionRange(pos, pos);
        } else if (elem.createTextRange) {
            var range = elem.createTextRange();
            range.collapse(true);
            range.moveEnd('character', pos);
            range.moveStart('character', pos);
            range.select();
        }
    });
    return this;
};

$.alert = function(title, message) {
    var div = $('<div title="' + title + '">').html(message);
    $("body").append(div);
    div.dialog();
};
$.urlParam = function(name) {
    var results = new RegExp('[\\?&]' + name + '=([^&#]*)').exec(window.location.href);
    if (!results) {
        return null;
    }
    return results[1] || null;
};
$.debug = function(obj, maxDepth, prefix) {
    var result = '';
    if (!prefix) prefix = '';
    for (var key in obj) {
        if (typeof obj[key] == 'object') {
            if (maxDepth !== undefined && maxDepth <= 1) {
                result += (prefix + key + '=object [max depth reached]<br>');
            } else {
                result += print(obj[key], (maxDepth) ? maxDepth - 1 : maxDepth, prefix + key + '.');
            }
        } else {
            result += (prefix + key + ' = ' + obj[key] + '<br>');
        }
    }
    outputSystemMessage(result);
};
$.htmlEncode = function(value) { return $('<div/>').text(value).html(); };
$.htmlDecode = function(value) { return $('<div/>').html(value).text(); };

//called by DetectWebcam.swf

function WebcamDetected(webcamdetected) {
    if (!videoChatEnabled || !webcamdetected) {
        $("#videoBroadcastButtonContainer").hide();
    } else {
        $("#videoBroadcastButtonContainer").show();
    }
}