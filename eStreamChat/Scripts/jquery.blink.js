(function ($) {
    var blinkTimers = new Object();

    $.fn.blink = function (options) {
        var defaults = { delay: 500, cssClass: 'ui-state-hover' };
        var options = $.extend(defaults, options);

        return this.each(function () {
            var obj = $(this);
            if (blinkTimers[obj] == undefined) {
                blinkTimers[obj] = setInterval(function () {
                    obj.toggleClass(options.cssClass);
                }, options.delay);
            }
        });
    };
    $.fn.stopBlink = function (options) {
        return this.each(function () {
            var obj = $(this);
            clearInterval(blinkTimers[obj]);
            delete blinkTimers[obj];
        });
    };
})(jQuery);