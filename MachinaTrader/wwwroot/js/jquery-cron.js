/*
 * Usage:
 *  (JS)
 *
 *  // initialise like this
 *  var c = $('#cron').cron({
 *    initial: '9 10 * * *', # Initial value. default = "* * * * *"
 *    url_set: '/set/', # POST expecting {"cron": "12 10 * * 6"}
 *  });
 *
 *  // you can update values later
 *  c.cron("value", "1 2 3 4 *");
 *
 * // you can also get the current value using the "value" option
 * alert(c.cron("value"));
 *
 *  (HTML)
 *  <div id='cron'></div>
 *
 * Notes:
 * At this stage, we only support a subset of possible cron options.
 * For example, each cron entry can only of the following form
 * ( - indicates where numbers should be replaced):
 * - Every minute : 0 0/1 * * * ?
 * - Every hour   : 0 - 0/1 * * ?
 * - Every day    : 0 - - * * ?
 * - Every week   : 0 - - ? * -
 * - Every month  : 0 - - - * ?
 * - Every year   : 0 - - - - ? *
 *
 * Ex.
 *    0 5 0/1 * * ?    => Every hour, five minutes past the hour
 *
 */
(function($) {
	var customCron = false;
	var customCronStr = "";
    var defaults = {
        initial : "0 0/1 * * * ?",
        minuteOpts : {
            minWidth  : 100, // only applies if columns and itemWidth not set
            itemWidth : 30,
            columns   : 4,
            rows      : undefined,
            title     : "Minutes Past the Hour"
        },
        timeHourOpts : {
            minWidth  : 100, // only applies if columns and itemWidth not set
            itemWidth : 20,
            columns   : 2,
            rows      : undefined,
            title     : "Time: Hour"
        },
        domOpts : {
            minWidth  : 100, // only applies if columns and itemWidth not set
            itemWidth : 30,
            columns   : undefined,
            rows      : 10,
            title     : "Day of Month"
        },
        monthOpts : {
            minWidth  : 100, // only applies if columns and itemWidth not set
            itemWidth : 100,
            columns   : 2,
            rows      : undefined,
            title     : undefined
        },
        dowOpts : {
            minWidth  : 100, // only applies if columns and itemWidth not set
            itemWidth : undefined,
            columns   : undefined,
            rows      : undefined,
            title     : undefined
        },
        timeMinuteOpts : {
            minWidth  : 100, // only applies if columns and itemWidth not set
            itemWidth : 20,
            columns   : 4,
            rows      : undefined,
            title     : "Time: Minute"
        },
        effectOpts : {
            openSpeed      : 400,
            closeSpeed     : 400,
            openEffect     : "slide",
            closeEffect    : "slide",
            hideOnMouseOut : true
        },
        url_set : undefined,
        customValues : undefined,
        onChange: undefined, // callback function each time value changes
        useGentleSelect: false
    };

    // -------  build some static data -------

    // options for minutes in an hour
    var strOptMih = "";
    var i;
    var j;
    for (i = 0; i < 60; i++) {
        j = (i < 10)? "0":"";
        strOptMih += "<option value='"+i+"'>" + j +  i + "</option>\n";
    }

    // options for hours in a day
    var strOptHid = "";
    for (i = 0; i < 24; i++) {
        j = (i < 10)? "0":"";
        strOptHid += "<option value='"+i+"'>" + j + i + "</option>\n";
    }

    // options for days of month
    var strOptDom = "";
    for (i = 1; i < 32; i++) {
        var suffix;
        if (i === 1 || i === 21 || i === 31) {
            suffix = "st";
        } else if (i === 2 || i === 22) {
            suffix = "nd";
        } else if (i === 3 || i === 23) {
            suffix = "rd";
        } else {
            suffix = "th";
        }
        strOptDom += "<option value='"+i+"'>" + i + suffix + "</option>\n";
    }

    // options for months
    var strOptMonth = "";
    var months = ["January", "February", "March", "April",
                  "May", "June", "July", "August",
                  "September", "October", "November", "December"];
    for (i = 0; i < months.length; i++) {
        strOptMonth += "<option value='"+(i+1)+"'>" + months[i] + "</option>\n";
    }

    // options for day of week
    var strOptDow = "";
    var days = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday",
                "Friday", "Saturday"];
    for (i = 0; i < days.length; i++) {
        strOptDow += "<option value='" + (i + 1) +"'>" + days[i] + "</option>\n";
    }

    // options for period
    var strOptPeriod = "";
    var periods = ["minute", "hour", "day", "week", "month", "year", "custom"];
    for (i = 0; i < periods.length; i++) {
        strOptPeriod += "<option value='"+periods[i]+"'>" + periods[i] + "</option>\n";
    }

    // display matrix
    var toDisplay = {
        "minute" : [],
        "hour"   : ["mins"],
        "day"    : ["time"],
        "week"   : ["dow", "time"],
        "month"  : ["dom", "time"],
        "year"   : ["dom", "month", "time"]
    };

    var combinations = {
        // Quartz Regex Expressions below                  // "-" indicates digit of one or two numbers that should be replaced with the desired time

        "minute" : /^0\s(0\/1)\s(\*\s){3}\?$/,             // "0 0/1 * * * ?"
        "hour"   : /^0\s\d{1,2}\s(0\/1)\s(\*\s){2}\?$/,    // "0 - 0/1 * * ?"
        "day"    : /^0\s(\d{1,2}\s){2}(\*\s){2}\?$/,       // "0 - - * * ?"
        "week"   : /^0\s(\d{1,2}\s){2}\?\s(\*\s)\d{1,2}$/, // "0 - - ? * -"
        "month"  : /^0\s(\d{1,2}\s){3}\*\s\?$/,            // "0 - - - * ?"
        "year"   : /^0\s(\d{1,2}\s){4}\?\s\*$/             // "0 - - - - ? *"
    };

    // ------------------ internal functions ---------------
    function defined(obj) {
        if (typeof obj == "undefined") { return false; }
        else { return true; }
    }

    function undefinedOrObject(obj) {
        return (!defined(obj) || typeof obj == "object");
    }

    function getCronType(cronStr, opts) {
        // if customValues defined, check for matches there first
        if (defined(opts.customValues)) {
            var key;
            for (key in opts.customValues) {
                if (opts.customValues.hasOwnProperty(key)) {
                    if (cronStr === opts.customValues[key]) {
                        return key;
                    }
                }
            }
        }

        // check format of initial cron value
        var validCron = /^0\s(0\/1|\d{1,2})\s(0\/1|\d{1,2}|\*)\s(\d{1,2}|\*|\?)\s(\d{1,2}|\*)\s(\d{1,2}|\?)(\s\*)?$/;
        if (typeof cronStr != "string" || !validCron.test(cronStr)) {
            //$.error("cron: invalid initial value");
            //return undefined;
			customCron = true;
			customCronStr = cronStr;
			return cronStr;
        }

        // check actual cron values
        var d = cronStr.split(" ");

        d = d.splice(1, d.length - 1);      // remove first 0

        //            mm, hh, DD, MM, DOW
        var minval = [ 0,  0,  1,  1,  1];
        var maxval = [59, 23, 31, 12,  7];
        for (var i = 0; i < d.length; i++) {
            if (d[i] === "*") continue;
            if (/^0\/1$/.test(d[i])) continue;
            if (d[i] === "?") continue;

            var v = parseInt(d[i]);
            if (defined(v) && v <= maxval[i] && v >= minval[i]) continue;

            $.error("cron: invalid value found (col "+(i+1)+") in " + window.o.initial);
            return undefined;
        }

        // determine combination
        for (var t in combinations) {
            if (combinations.hasOwnProperty(t)) {
                if (combinations[t].test(cronStr)) {
                    return t;
                }
            }
        }

        // unknown combination
        $.error("cron: valid but unsupported cron format. sorry.");
        return undefined;
    }

    function hasError(c, o) {
        if (!defined(getCronType(o.initial, o))) { return true; }
        if (!undefinedOrObject(o.customValues)) { return true; }

        // ensure that customValues keys do not coincide with existing fields
        if (defined(o.customValues)) {
            var key;
            for (key in o.customValues) {
                if (combinations.hasOwnProperty(key)) {
                    $.error("cron: reserved keyword '" + key +
                            "' should not be used as customValues key.");
                    return true;
                }
            }
        }

        return false;
    }

    function getCurrentValue(c) {
        var b = c.data("block");
        var hour;
        var day;
        var month;
        var dow;
        var min = hour = day = month = dow = "*";
        var selectedPeriod = b["period"].find("select").val();

        switch (selectedPeriod) {
            case "minute":
                return ["0", "0/1", "*", "*", "*", "?"].join(" ");
            case "hour":
                min = b["mins"].find("select").val();
                return ["0", min, "0/1", "*", "*", "?"].join(" ");
            case "day":
                min  = b["time"].find("select.cron-time-min").val();
                hour = b["time"].find("select.cron-time-hour").val();
                return ["0", min, hour, "*", "*", "?"].join(" ");
            case "week":
                min  = b["time"].find("select.cron-time-min").val();
                hour = b["time"].find("select.cron-time-hour").val();
                dow  =  b["dow"].find("select").val();
                return ["0", min, hour, "?", "*", dow].join(" ");
            case "month":
                min  = b["time"].find("select.cron-time-min").val();
                hour = b["time"].find("select.cron-time-hour").val();
                day  = b["dom"].find("select").val();
                return ["0", min, hour, day, "*", "?"].join(" ");
            case "year":
                min  = b["time"].find("select.cron-time-min").val();
                hour = b["time"].find("select.cron-time-hour").val();
                day  = b["dom"].find("select").val();
                month = b["month"].find("select").val();
                return ["0", min, hour, day, month, "?", "*"].join(" ");
            case "custom":
				return customCronStr;
            default:
                // we assume this only happens when customValues is set
                return selectedPeriod;
        }

    }

    // -------------------  PUBLIC METHODS -----------------

    var eventHandlers;
    var methods = {
        init : function(opts) {

            // init options
            var options = opts ? opts : {}; /* default to empty obj */
            var o = $.extend([], defaults, options);
            var eo = $.extend({}, defaults.effectOpts, options.effectOpts);
            $.extend(o, {
                minuteOpts     : $.extend({}, defaults.minuteOpts, eo, options.minuteOpts),
                domOpts        : $.extend({}, defaults.domOpts, eo, options.domOpts),
                monthOpts      : $.extend({}, defaults.monthOpts, eo, options.monthOpts),
                dowOpts        : $.extend({}, defaults.dowOpts, eo, options.dowOpts),
                timeHourOpts   : $.extend({}, defaults.timeHourOpts, eo, options.timeHourOpts),
                timeMinuteOpts : $.extend({}, defaults.timeMinuteOpts, eo, options.timeMinuteOpts)
            });

            // error checking
            if (hasError(this, o)) { return this; }

            // ---- define select boxes in the right order -----

            var block = [], customPeriods = "", cv = o.customValues;
            if (defined(cv)) { // prepend custom values if specified
                for (var key in cv) {
                    if (cv.hasOwnProperty(key)) {
                        customPeriods += "<option value='" + cv[key] + "'>" + key + "</option>\n";
                    }
                }
            }

            block["period"] = $("<span class='cron-period'>"
                    + "Every <select class='form-control' name='cron-period'>" + customPeriods
                    + strOptPeriod + "</select> </span>")
                .appendTo(this)
                .data("root", this);

            var select = block["period"].find("select");
            select.bind("change.cron", eventHandlers.periodChanged)
                  .data("root", this);
            if (o.useGentleSelect) select.gentleSelect(eo);

            block["dom"] = $("<span class='cron-block cron-block-dom'>"
                    + " on the <select class='form-control' name='cron-dom'>" + strOptDom
                    + "</select> </span>")
                .appendTo(this)
                .data("root", this);

            select = block["dom"].find("select").data("root", this);
            if (o.useGentleSelect) select.gentleSelect(o.domOpts);

            block["month"] = $("<span class='cron-block cron-block-month'>"
                    + " of <select class='form-control' name='cron-month'>" + strOptMonth
                    + "</select> </span>")
                .appendTo(this)
                .data("root", this);

            select = block["month"].find("select").data("root", this);
            if (o.useGentleSelect) select.gentleSelect(o.monthOpts);

            block["mins"] = $("<span class='cron-block cron-block-mins'>"
                    + " at <select class='form-control' name='cron-mins'>" + strOptMih
                    + "</select> minutes past the hour </span>")
                .appendTo(this)
                .data("root", this);

            select = block["mins"].find("select").data("root", this);
            if (o.useGentleSelect) select.gentleSelect(o.minuteOpts);

            block["dow"] = $("<span class='cron-block cron-block-dow'>"
                    + " on <select class='form-control' name='cron-dow'>" + strOptDow
                    + "</select> </span>")
                .appendTo(this)
                .data("root", this);

            select = block["dow"].find("select").data("root", this);
            if (o.useGentleSelect) select.gentleSelect(o.dowOpts);

            block["time"] = $("<span class='cron-block cron-block-time'>"
                    + " at <select class='form-control cron-time-hour' name='cron-time-hour'>" + strOptHid
                    + "</select>:<select class='form-control cron-time-min' name='cron-time-min'>" + strOptMih
                    + " </span>")
                .appendTo(this)
                .data("root", this);

            select = block["time"].find("select.cron-time-hour").data("root", this);
            if (o.useGentleSelect) select.gentleSelect(o.timeHourOpts);
            select = block["time"].find("select.cron-time-min").data("root", this);
            if (o.useGentleSelect) select.gentleSelect(o.timeMinuteOpts);

            block["controls"] = $("<span class='cron-controls'>&laquo; save "
                    + "<span class='cron-button cron-button-save'></span>"
                    + " </span>")
                .appendTo(this)
                .data("root", this)
                .find("span.cron-button-save")
                    .bind("click.cron", eventHandlers.saveClicked)
                    .data("root", this)
                    .end();

            this.find("select").bind("change.cron-callback", eventHandlers.somethingChanged);
            this.data("options", o).data("block", block); // store options and block pointer
            this.data("current_value", o.initial); // remember base value to detect changes
            return methods["value"].call(this, o.initial); // set initial value
        },

        value : function(cronStr) {
            // when no args, act as getter
            if (!cronStr) { return getCurrentValue(this); }

            var o = this.data('options');
            var block = this.data("block");
            var useGentleSelect = o.useGentleSelect;
            var t = getCronType(cronStr, o);

            if (!defined(t)) { return false; }

            var bp;
            if (defined(o.customValues) && o.customValues.hasOwnProperty(t)) {
                t = o.customValues[t];
            } else {
                var d = cronStr.split(" ");

                d = d.splice(1, d.length - 1);  // Remove first 0

                var i;
                for (i = 0; i < d.length; i++) {                // Remove non digits
                    if(/^0\/1$/.test(d[i])) { d[i] = undefined; }
                    if(d[i] === '?') { d[i] = undefined; }
                }

                var v = {
                    "mins"  : d[0],
                    "hour"  : d[1],
                    "dom"   : d[2],
                    "month" : d[3],
                    "dow"   : d[4]
                };

                // update appropriate select boxes
                var targets = toDisplay[t];
				if (customCron){
				    bp = block["period"].find("select").val("custom");
				    if (useGentleSelect) bp.gentleSelect("update");
					bp.trigger("change");					
					return this;
				}
                for (i = 0; i < targets.length; i++) {
                    var tgt = targets[i];
                    var btgt;
                    if (tgt === "time") {
                        btgt = block[tgt].find("select.cron-time-hour").val(v["hour"]);
                        if (useGentleSelect) btgt.gentleSelect("update");

                        btgt = block[tgt].find("select.cron-time-min").val(v["mins"]);
                        if (useGentleSelect) btgt.gentleSelect("update");
                    } else {;
                        btgt = block[tgt].find("select").val(v[tgt]);
                        if (useGentleSelect) btgt.gentleSelect("update");
                    }
                }
            }

            // trigger change event
            bp = block["period"].find("select").val(t);
            if (useGentleSelect) bp.gentleSelect("update");
            bp.trigger("change");

            return this;
        }

    };

    eventHandlers = {
        periodChanged : function() {
            var root = $(this).data("root");
            var block = root.data("block");
            // opt = root.data("options");
            var period = $(this).val();

            root.find("span.cron-block").hide(); // first, hide all blocks
            if (toDisplay.hasOwnProperty(period)) { // not custom value
                var b = toDisplay[$(this).val()];
                for (var i = 0; i < b.length; i++) {
                    block[b[i]].show();
                }
            }
        },

        somethingChanged : function() {
            var root = $(this).data("root");
            // if AJAX url defined, show "save"/"reset" button
            if (defined(root.data("options").url_set)) {
                if (methods.value.call(root) !== root.data("current_value")) { // if changed
                    root.addClass("cron-changed");
                    root.data("block")["controls"].fadeIn();
                } else { // values manually reverted
                    root.removeClass("cron-changed");
                    root.data("block")["controls"].fadeOut();
                }
            } else {
                root.data("block")["controls"].hide();
            }

            // chain in user defined event handler, if specified
            var oc = root.data("options").onChange;
            if (defined(oc) && $.isFunction(oc)) {
                oc.call(root);
            }
        },

        saveClicked : function() {
            var btn  = $(this);
            var root = btn.data("root");
            var cronStr = methods.value.call(root);

            if (btn.hasClass("cron-loading")) { return; } // in progress
            btn.addClass("cron-loading");

            $.ajax({
                type : "POST",
                url  : root.data("options").url_set,
                data : { "cron" : cronStr },
                success : function() {
                    root.data("current_value", cronStr);
                    btn.removeClass("cron-loading");
                    // data changed since "save" clicked?
                    if (cronStr === methods.value.call(root)) {
                        root.removeClass("cron-changed");
                        root.data("block").controls.fadeOut();
                    }
                },
                error : function() {
                    alert("An error occured when submitting your request. Try again?");
                    btn.removeClass("cron-loading");
                }
            });
        }
    };

    $.fn.cron = function(method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || ! method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error( 'Method ' +  method + ' does not exist on jQuery.cron' );
        }
        return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
    };

})(jQuery);
