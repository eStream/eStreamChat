var emoticons = {
    "emoticon": {
        "::Very Happy": {
            "image": "biggrin.gif",
            "emotes": {
                ":D": "",
                ":-D": "",
                ":grin:": "",
                ":biggrin:": ""
            }
        },
        "::Smile": {
            "image": "smile.gif",
            "emotes": {
                ":)": "",
                ":-)": "",
                ":smile:": ""
            }
        },
        "::Sad": {
            "image": "sad.gif",
            "emotes": {
                ":(": "",
                ":-(": "",
                ":sad:": ""
            }
        },
        "::Surprised": {
            "image": "surprised.gif",
            "emotes": {
                ":o": "",
                ":-o": "",
                ":eek:": ""
            }
        },
        "::Shock": {
            "image": "shock.gif",
            "emotes": {
                ":shock:": ""
            }
        },
        "::Confused": {
            "image": "confused.gif",
            "emotes": {
                ":?": "",
                ":-?": "",
                ":???:": ""
            }
        },
        "::Cool": {
            "image": "cool.gif",
            "emotes": {
                "8)": "",
                "8-)": "",
                ":cool:": ""
            }
        },
        "::Laughing": {
            "image": "lol.gif",
            "emotes": {
                ":lol:": ""
            }
        },
        "::Mad": {
            "image": "mad.gif",
            "emotes": {
                ":x": "",
                ":-X": "",
                ":mad:": ""
            }
        },
        "::Razz": {
            "image": "razz.gif",
            "emotes": {
                ":p": "",
                ":-p": "",
                ":P": "",
                ":-P": "",
                ":razz:": ""
            }
        },
        "::Embarassed": {
            "image": "redface.gif",
            "emotes": {
                ":oops:": ""
            }
        },
        "::Crying or Very sad": {
            "image": "cry.gif",
            "emotes": {
                ":cry:": ""
            }
        },
        "::Evil or Very Mad": {
            "image": "evil.gif",
            "emotes": {
                ":evil:": ""
            }
        },
        "::Bad Grin": {
            "image": "badgrin.gif",
            "emotes": {
                ":badgrin:": ""
            }
        },
        "::Rolling Eyes": {
            "image": "rolleyes.gif",
            "emotes": {
                ":roll:": ""
            }
        },
        "::Wink": {
            "image": "wink.gif",
            "emotes": {
                ";)": "",
                ";-)": "",
                ":wink:": ""
            }
        },
        "::Exclamation": {
            "image": "exclaim.gif",
            "emotes": {
                ":!:": ""
            }
        },
        "::Question": {
            "image": "question.gif",
            "emotes": {
                ":?:": ""
            }
        },
        "::Idea": {
            "image": "idea.gif",
            "emotes": {
                ":idea:": ""
            }
        },
        "::Arrow": {
            "image": "arrow.gif",
            "emotes": {
                ":arrow:": ""
            }
        },
        "::Neutral": {
            "image": "neutral.gif",
            "emotes": {
                ":|": "",
                ":-|": "",
                ":neutral:": ""
            }
        },
        "::Doubt": {
            "image": "doubt.gif",
            "emotes": {
                ":doubt:": ""
            }
        },
        "::Applause": {
            "image": "clap.gif",
            "emotes": {
                "=D&gt;": ""
            }
        },
        "::doh!": {
            "image": "doh.gif",
            "emotes": {
                "#-o": ""
            }
        },
        "::Drool": {
            "image": "drool.gif",
            "emotes": {
                "=P~": ""
            }
        },
        "::Liar": {
            "image": "liar.gif",
            "emotes": {
                ":^o": "",
                ":---)": ""
            }
        },
        "::Shame on you": {
            "image": "naughty.gif",
            "emotes": {
                "[-X": ""
            }
        },
        "::Pray": {
            "image": "pray.gif",
            "emotes": {
                "[-o&lt;": ""
            }
        },
        "::Anxious": {
            "image": "shifty.gif",
            "emotes": {
                "8-[": ""
            }
        },
        "::Not talking": {
            "image": "snooty.gif",
            "emotes": {
                "[-(": ""
            }
        },
        "::Think": {
            "image": "think.gif",
            "emotes": {
                ":-k": ""
            }
        },
        "::Brick wall": {
            "image": "wall.gif",
            "emotes": {
                "](*,)": ""
            }
        },
        "::Whistle": {
            "image": "whistle.gif",
            "emotes": {
                ":whistle:": ""
            }
        },
        "::Angel": {
            "image": "angel.gif",
            "emotes": {
                "O:)": ""
            }
        },
        "::Speak to the hand": {
            "image": "hand.gif",
            "emotes": {
                "=;": ""
            }
        },
        "::Sick": {
            "image": "sick.gif",
            "emotes": {
                ":sick:": ""
            }
        },
        "::Shhh": {
            "image": "shhh.gif",
            "emotes": {
                ":-$": ""
            }
        },
        "::Eh?": {
            "image": "eh.gif",
            "emotes": {
                ":-s": ""
            }
        },
        "::Dancing": {
            "image": "dance.gif",
            "emotes": {
                ":D|": ""
            }
        },
        "::Silenced": {
            "image": "silenced.gif",
            "emotes": {
                ":-#": ""
            }
        }
    }
};

RegExp.escape = function (text) {
    if (!arguments.callee.sRE) {
        var specials = [
      '/', '.', '*', '+', '?', '|',
      '(', ')', '[', ']', '{', '}', '\\'
    ];
        arguments.callee.sRE = new RegExp(
      '(\\' + specials.join('|\\') + ')', 'g'
    );
    }
    return text.replace(arguments.callee.sRE, '\\$1');
}

$.fn.emoticon = function (theText) {
    var imagePath = "Emoticons/";
    var newText = theText;
    for (var a in emoticons.emoticon) {
        emoticon = emoticons.emoticon[a];
        for (var emote in emoticon.emotes) {

            emote = RegExp.escape(emote);
            newText = newText.replace(new RegExp(emote, 'gi'), '<img src="' + imagePath + emoticon.image + '" />');
        }
    }
    return newText;
};