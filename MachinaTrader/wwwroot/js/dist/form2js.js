/**
 * Copyright (c) 2018 Thiemo Borger
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 * @author Thiemo Borger
 * Date: 20.07.2018
 * Time: 06:05:00
 */
(function (root, factory) {
  if (typeof define === 'function' && define.amd) {
    // AMD. Register as an anonymous module.
    define(factory);
  } else {
    // Browser globals
    root.form2js = factory();
  }
})(this, function () {
  "use strict";
  /**
   * Returns form values represented as Javascript object
   * "name" attribute defines structure of resulting object
   *
   * @param rootNode {Element|String} root form element (or it's id) or array of root elements
   * @param delimiter {String} structure parts delimiter defaults to '.'
   * @param skipEmpty {Boolean} should skip empty text values, defaults to true
   * @param nodeCallback {Function} custom function to get node value
   * @param useIdIfEmptyName {Boolean} if true value of id attribute of field will be used if name of field is empty
   */

  var skipHidden = false;

  function form2js(rootNode, delimiter, skipEmpty, nodeCallback, skipHiddenVar, useIdIfEmptyName) {
    if (typeof skipEmpty === 'undefined' || skipEmpty === null) skipEmpty = true;
    if (typeof delimiter === 'undefined' || delimiter === null) delimiter = '.';
    if (arguments.length < 5) useIdIfEmptyName = false;
    skipHidden = skipHiddenVar;
    if (arguments.length < 6) useIdIfEmptyName = false;
    rootNode = typeof rootNode === 'string' ? document.getElementById(rootNode) : rootNode;
    var formValues = [],
        currNode,
        i = 0;
    /* If rootNode is array - combine values */

    if (rootNode.constructor === Array || typeof NodeList !== "undefined" && rootNode.constructor === NodeList) {
      while (currNode === rootNode[i++]) {
        formValues = formValues.concat(getFormValues(currNode, nodeCallback, useIdIfEmptyName));
      }
    } else {
      formValues = getFormValues(rootNode, nodeCallback, useIdIfEmptyName);
    }

    return processNameValues(formValues, skipEmpty, delimiter);
  }

  function processNameValues(nameValues, skipEmpty, delimiter) {
    var result = {},
        arrays = {},
        i,
        j,
        k,
        l,
        value,
        nameParts,
        currResult,
        arrNameFull,
        arrName,
        arrIdx,
        namePart,
        name,
        _nameParts;

    for (i = 0; i < nameValues.length; i++) {
      value = nameValues[i].value;

      if (value === 'FORCE_EMPTY_NULL') {
        value = null;
      } else if (value === 'FORCE_EMPTY_STRING') {
        value = "";
      } else if (skipEmpty && (value === '' || value === null)) {
        continue;
      }

      name = nameValues[i].name;
      _nameParts = name.split(delimiter);
      nameParts = [];
      currResult = result;
      arrNameFull = '';

      for (j = 0; j < _nameParts.length; j++) {
        namePart = _nameParts[j].split('][');

        if (namePart.length > 1) {
          for (k = 0; k < namePart.length; k++) {
            if (k === 0) {
              namePart[k] = namePart[k] + ']';
            } else if (k === namePart.length - 1) {
              namePart[k] = '[' + namePart[k];
            } else {
              namePart[k] = '[' + namePart[k] + ']';
            }

            arrIdx = namePart[k].match(/([a-z_]+)?\[([a-z_][a-z0-9_]+?)\]/i);

            if (arrIdx) {
              for (l = 1; l < arrIdx.length; l++) {
                if (arrIdx[l]) nameParts.push(arrIdx[l]);
              }
            } else {
              nameParts.push(namePart[k]);
            }
          }
        } else nameParts = nameParts.concat(namePart);
      }

      for (j = 0; j < nameParts.length; j++) {
        namePart = nameParts[j];

        if (namePart.indexOf('[]') > -1 && j === nameParts.length - 1) {
          arrName = namePart.substr(0, namePart.indexOf('['));
          arrNameFull += arrName;
          if (!currResult[arrName]) currResult[arrName] = [];
          currResult[arrName].push(value);
        } else if (namePart.indexOf('[') > -1) {
          arrName = namePart.substr(0, namePart.indexOf('['));
          arrIdx = namePart.replace(/(^([a-z_]+)?\[)|(\]$)/gi, '');
          /* Unique array name */

          arrNameFull += '_' + arrName + '_' + arrIdx;
          /*
           * Because arrIdx in field name can be not zero-based and step can be
           * other than 1, we can't use them in target array directly.
           * Instead we're making a hash where key is arrIdx and value is a reference to
           * added array element
           */

          if (!arrays[arrNameFull]) arrays[arrNameFull] = {};
          if (arrName !== '' && !currResult[arrName]) currResult[arrName] = [];

          if (j === nameParts.length - 1) {
            if (arrName === '') {
              currResult.push(value);
              arrays[arrNameFull][arrIdx] = currResult[currResult.length - 1];
            } else {
              currResult[arrName].push(value);
              arrays[arrNameFull][arrIdx] = currResult[arrName][currResult[arrName].length - 1];
            }
          } else {
            if (!arrays[arrNameFull][arrIdx]) {
              if (/^[a-z_]+\[?/i.test(nameParts[j + 1])) currResult[arrName].push({});else currResult[arrName].push([]);
              arrays[arrNameFull][arrIdx] = currResult[arrName][currResult[arrName].length - 1];
            }
          }

          currResult = arrays[arrNameFull][arrIdx];
        } else {
          arrNameFull += namePart;

          if (j < nameParts.length - 1)
            /* Not the last part of name - means object */
            {
              if (!currResult[namePart]) currResult[namePart] = {};
              currResult = currResult[namePart];
            } else {
            currResult[namePart] = value;
          }
        }
      }
    }

    return result;
  }

  function getFormValues(rootNode, nodeCallback, useIdIfEmptyName) {
    var result = extractNodeValues(rootNode, nodeCallback, useIdIfEmptyName);
    return result.length > 0 ? result : getSubFormValues(rootNode, nodeCallback, useIdIfEmptyName);
  }

  function getSubFormValues(rootNode, nodeCallback, useIdIfEmptyName) {
    var result = [],
        currentNode = rootNode.firstChild;

    while (currentNode) {
      result = result.concat(extractNodeValues(currentNode, nodeCallback, useIdIfEmptyName));
      currentNode = currentNode.nextSibling;
    }

    return result;
  }

  function extractNodeValues(node, nodeCallback, useIdIfEmptyName) {
    var fieldValue,
        result,
        fieldName = getFieldName(node, useIdIfEmptyName);
    var callbackResult = nodeCallback && nodeCallback(node);

    if (callbackResult && callbackResult.name) {
      result = [callbackResult];
    } else if (fieldName !== '' && node.nodeName.match(/INPUT|TEXTAREA/i)) {
      fieldValue = getFieldValue(node);
      result = [{
        name: fieldName,
        value: fieldValue
      }];
    } else if (fieldName !== '' && node.nodeName.match(/SELECT/i)) {
      fieldValue = getFieldValue(node);
      result = [{
        name: fieldName.replace(/\[\]$/, ''),
        value: fieldValue
      }];
    } else {
      result = getSubFormValues(node, nodeCallback, useIdIfEmptyName);
    }

    return result;
  }

  function getFieldName(node, useIdIfEmptyName) {
    if (node.name && node.name !== '') return node.name;else if (useIdIfEmptyName && node.id && node.id !== '') return node.id;else return '';
  }

  function getFieldValueType(fieldNode) {
    if ($(fieldNode).hasClass("float")) {
      return parseFloat(fieldNode.value);
    }

    if ($(fieldNode).hasClass("integer")) {
      return parseInt(fieldNode.value);
    }
    /*if ($(fieldNode).hasClass("array")) {
        return fieldNode.value.split(",");
    }*/


    if ($(fieldNode).hasClass("array")) {
      var selectedValues = [];
      $(fieldNode).each(function () {
        selectedValues.push($(this).val());
      });
      return selectedValues;
    }

    if ($(fieldNode).hasClass("bool")) {
      if (fieldNode.value === "true" || fieldNode.value === "on" || fieldNode.value === "checked") {
        return true;
      } else if (fieldNode.value === "false" || fieldNode.value === "off" || fieldNode.value === "unchecked") {
        return false;
      } else {
        return fieldNode.value;
      }
    }

    if ($(fieldNode).hasClass("string")) {
      if (fieldNode.value === '' || fieldNode.value === null) {
        return String("FORCE_EMPTY_STRING");
      } else {
        return fieldNode.value;
      }
    }

    return fieldNode.value;
  }

  function getFieldValue(fieldNode) {
    if (skipHidden) {
      if (!$(fieldNode).is(":visible") && !$(fieldNode).hasClass("form2jsEnable")) return null;
    }

    if (fieldNode.disabled && !$(fieldNode).hasClass("form2jsEnable")) return null;
    if ($(fieldNode).hasClass("form2jsDisable")) return null;

    switch (fieldNode.type.toLowerCase()) {
      case 'text':
        return getFieldValueType(fieldNode);

      case 'hidden':
        return getFieldValueType(fieldNode);

      case 'textarea':
        return getFieldValueType(fieldNode);

      case 'select':
        return getSelectedOptionValue(fieldNode);

      case 'select-multiple':
        return getSelectedOptionValue(fieldNode);

      case 'radio':
        if (!fieldNode.checked) return null;
        if (fieldNode.checked && fieldNode.value === "false") return false;
        return fieldNode.value;

      case 'checkbox':
        if ($(fieldNode).hasClass("bool")) {
          if (fieldNode.checked) {
            return true;
          } else {
            return false;
          }
        } else {
          if (fieldNode.checked && fieldNode.value === "null") return String("FORCE_EMPTY_NULL");
          if (fieldNode.checked && fieldNode.value === "true") return true;
          if (!fieldNode.checked && fieldNode.value === "true") return false;
          if (fieldNode.checked) return fieldNode.value;
        }

        break;

      case 'button':
      case 'reset':
      case 'submit':
      case 'image':
        return null;

      default:
        return fieldNode.value;
    }

    return null;
  }

  function getSelectedOptionValue(selectNode) {
    var multiple = selectNode.multiple,
        result = [],
        options,
        i,
        l;

    if (!multiple) {
      if ($(selectNode).hasClass("float")) {
        return parseFloat(selectNode.value);
      }

      if ($(selectNode).hasClass("integer")) {
        return parseInt(selectNode.value);
      }

      return selectNode.value;
    }

    for (options = selectNode.getElementsByTagName("option"), i = 0, l = options.length; i < l; i++) {
      if (options[i].selected) result.push(options[i].value);
    }

    return result;
  }

  return form2js;
});
//# sourceMappingURL=form2js.js.map