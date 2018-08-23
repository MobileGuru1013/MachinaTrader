// Wire up signalr and js intervals -> preparePage() is hooked from navigation script on page change

/* eslint-disable */
function getParam(name) {
  name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
  var regexS = "[\\?&]" + name + "=([^&#]*)";
  var regex = new RegExp(regexS);
  var results = regex.exec(window.location.href);
  if (results === null) return null;else return results[1];
}

function selectPickerImage(opt) {
  if (!opt.id) {
    return opt.text;
  }

  var optimage = $(opt.element).data('image');
  var opticon = $(opt.element).data('icon');

  if (!optimage && !opticon) {
    return opt.text;
  }

  if (optimage) {
    var $opt = $('<span class="userName"><img style="width: 16px;" src="' + optimage + '" class="dropdownImage" />&nbsp;&nbsp;' + $(opt.element).text() + '</span>');
    return $opt;
  }

  if (opticon) {
    var $opt = $('<span class="userName"><i class="' + opticon + '"/></i>&nbsp;&nbsp;' + $(opt.element).text() + '</span>');
    return $opt;
  }
}

;
var jsInterval = {};
var signalrConnections = {};

function runJsInterval(script, str, delay) {
  if (!jsInterval[str]) {
    // console.log("Not in array")
    jsInterval[str] = setInterval(script, delay);
  }
}

function clearJsInterval() {
  for (var key in jsInterval) {
    if (Object.prototype.hasOwnProperty.call(jsInterval, key)) {
      clearInterval(jsInterval[key]);
      jsInterval[key] = null;
      delete jsInterval[key];
    }
  }
}
/* eslint-disable no-unused-vars */


function addSignalrClient(name, connection) {
  if (!signalrConnections[name]) {
    // console.log("Not in array")
    signalrConnections[name] = connection;
  }
}

function cleanSignalr() {
  for (var key in signalrConnections) {
    if (Object.prototype.hasOwnProperty.call(signalrConnections, key)) {
      // console.log("Disconnect")
      // console.log(signalrConnections[key])
      signalrConnections[key].stop();
      delete signalrConnections[key];
    }
  }
}

function beforeHook() {
  // Hacky way to remove intervals

  /*for(i=0; i<100; i++)
  {
  	window.clearInterval(i);
  }*/
  // Destroy all loaded vuejs components
  for (var key in vueMain.$children) {
    vueMain.$children[key].$destroy();
  } //Cleanup all keydown listeners -> Needed for some JS components


  $(document).off("keydown");
  clearJsInterval();
  cleanSignalr();
}

function afterHook() {}

function processDate(node) {
  return false;
}

function postFormData(formId, submitUrl, reload) {
  if (typeof reload === 'undefined') reload = true;
  var formDataField = form2js(formId, '.', true, processDate);
  $.ajax({
    type: "POST",
    url: submitUrl,
    cache: false,
    contentType: "application/json",
    dataType: 'json',
    data: JSON.stringify(formDataField, null, null)
  });

  if (reload === true) {
    setTimeout('location.reload();', 100);
  }
}

function postFormDataSkipHidden(formId, submitUrl, reload) {
  if (typeof reload === 'undefined') reload = true;
  var formDataField = form2js(formId, '.', true, processDate, true);
  console.log(formDataField);
  $.ajax({
    type: "POST",
    url: submitUrl,
    cache: false,
    contentType: "application/json",
    dataType: 'json',
    data: JSON.stringify(formDataField, null, null)
  });

  if (reload === true) {
    setTimeout('location.reload();', 100);
  }
}
//# sourceMappingURL=global.js.map