<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ChatRoom.aspx.cs" Inherits="eStreamChat.ChatRoomPage" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>eStreamChat</title>
    <link href="Styles/Style.css" rel="stylesheet" type="text/css" />
    <link href="Styles/jquery.contextMenu.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <script src="http://code.jquery.com/jquery.min.js" type="text/javascript"></script>
    <script src="http://code.jquery.com/ui/1.8.23/jquery-ui.min.js" type="text/javascript"></script>
    <script src="Scripts/jquery.jqote2.js" type="text/javascript"></script>
    <script src="Scripts/jquery.blink.js" type="text/javascript"></script>
    <script src="Scripts/jquery.colorPicker.js" type="text/javascript"></script>
    <script src="Scripts/jquery.emoticon.js" type="text/javascript"></script>
    <script src="Scripts/jquery.ajaxfileupload.js" type="text/javascript"></script>
    <script src="Scripts/jquery.contextMenu.mod.js" type="text/javascript"></script>
    <script src="Scripts/eStreamChat.js" type="text/javascript"></script>
    <script src="App_Themes/<%= Theme %>/layout.js" type="text/javascript"></script>
    <span id="templates" data-url="App_Themes/<%= Theme %>/templates.html" style="display:none"></span>
    <bgsound id="sound" />
    <span id="spanSound"></span>
    <ul id="adminMenu" class="contextMenu">
        <li class="private-chat"><a href="#private_chat">Chat</a></li>
        <li class="view-profile"><a href="#view_profile">Profile</a></li>
        <li class="ignore-user menu-separator"><a href="#ignore_user">Ignore</a></li>
        <li class="kick-user"><a href="#kick_user">Kick</a></li>
        <li class="ban-user"><a href="#ban_user">Ban</a></li>
        <li class="slap-user menu-separator"><a href="#slap_user">Slap!</a></li>
    </ul>
    <ul id="userMenu" class="contextMenu">
        <li class="private-chat"><a href="#private_chat">Chat</a></li>
        <li class="view-profile"><a href="#view_profile">Profile</a></li>
        <li class="ignore-user"><a href="#ignore_user">Ignore</a></li>
        <li class="slap-user menu-separator"><a href="#slap_user">Slap!</a></li>
    </ul>
    <div id="fileUploadDialog" title="Send File" style="display: none">
        <p><input type="file" id="fileUpload" name="fileUpload" size="23"/></p>
        <p><button id="uploadButton">Upload</button></p>     
    </div>
    <div id="container">
        <form id="form1" runat="server">
        <div id="videosPlaceholder" style="position: absolute; z-index: 888888;"></div>
        <div id="header"><img src="Styles/logo.png"/></div>
        <div class="content">
            <div id="section">
            <div id="tabs">
                    <ul><li><a href="#panel-room">Room</a></li></ul>
                    <div id="panel-room" class="messages"></div>
            </div>
        </div>
            <div id="aside">
                <div class="aside-header">Users online</div>
                <div id="onlineUsers" class="aside-content"></div>
            </div>
        </div>
        <div id="footer">
            <div id="button-panel" class="text_format">
                <span id="formatButtons" class="text-icons">
                <input type="checkbox" id="checkBold" /><label for="checkBold" class="bold"></label>
                <input type="checkbox" id="checkItalic" /><label for="checkItalic" class="italic"></label>
                <input type="checkbox" id="checkUnderline" /><label for="checkUnderline" class="underline"></label>
            </span>
            <span class="color_picker_wrap"><input id="textColor" type="text" value="#000000" /></span>
                <select id="dropFontName" class="ui-button ui-widget ui-state-default ui-corner-left">
                    <option value="">Font</option>
                    <option value="Arial">Arial</option>
                    <option value="Verdana">Verdana</option>
                    <option value="Wingdings">Wingdings</option>
                    <option value="Courier">Courier</option>
                    <option value="Impact">Impact</option>
                    <option value="Georgia">Georgia</option>
                    <option value="Comic Sans MS">Comic Sans MS</option>
                </select>
                <select id="dropFontSize" class="ui-button ui-widget ui-state-default  ui-corner-right">
                    <option value="">Size</option>
                    <option value="7">7</option>
                    <option value="8">8</option>
                    <option value="9">9</option>
                    <option value="10">10</option>
                    <option value="11">11</option>
                    <option value="12">12</option>
                    <option value="14">14</option>
                    <option value="15">15</option>
                    <option value="16">16</option>
                    <option value="18">18</option>
                    <option value="20">20</option>
                    <option value="22">22</option>
                    <option value="28">28</option>
                    <option value="32">32</option>
                </select>
            	<span class="options_btn"><input type="button" id="fileUploadDialogButton" class="send_file options_btn" title="send file" /></span>
                <span class="text-icons"><input type="checkbox" id="checkAlert" /><label for="checkAlert" class="alert"></label>
                </span>
                <span id="videoBroadcastButtonContainer" class="options_btn" ><input type="button" class="broadcast_video" id="checkVideoBroadcast" title="Broadcast video" /></span>
                <!-- Powered by label can be removed with commercial license -->
                <a class="poweredby" href="http://www.estreamchat.com" target="_blank"></a>
            </div>
            <div class="send_msg">
                <input id="messageInput" type="text" />
                <button id="sendButton">Send</button>
        </div>
        </div>
        <div id="webcamdetector"></div>
        </form>
    </div>
</body>
</html>
