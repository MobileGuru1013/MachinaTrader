#!/usr/bin/env node

'use strict'

const fs         = require('fs')
const path       = require('path')
const mkdirp     = require('mkdirp')
const sh         = require('shelljs')
const uglifyjs   = require('uglify-js');
const cleancss   = require('clean-css');
const { promisify } = require('util');

const readFileAsync  = promisify(fs.readFile);
const writeFileAsync = promisify(fs.writeFile)

const basename   = path.basename
const dirname    = path.dirname
const resolve    = path.resolve
const normalize  = path.normalize
const join       = path.join
const relative   = path.relative
const extension  = path.extname
const globule    = require('globule');

const src        = 'wwwroot/'
const dest       = 'wwwroot/'
const base       = path.resolve(__dirname, '..')

const walkSync = (dir, filelist = []) => {
  fs.readdirSync(dir).forEach(file => {
    filelist = fs.statSync(path.join(dir, file)).isDirectory()
    ? walkSync(path.join(dir, file), filelist)
    : filelist.concat(path.join(dir, file))
  })
  return filelist
}

const vendorName = (path) => {
  const nodeModules = Boolean(path.split('/')[0] === 'node_modules')
  const subDir = Boolean(path.indexOf('@') >= 0)
  let vendor
  if (nodeModules) {
    if (subDir) {
      vendor = `${path.split('/')[1]}/${path.split('/')[2]}`
    } else {
      vendor = `${path.split('/')[1]}`
    }
  }
  return vendor
}

function removeDuplicates( arr, prop ) {
  let obj = {};
  return Object.keys(arr.reduce((prev, next) => {
    if(!obj[next[prop]]) obj[next[prop]] = next;
    return obj;
  }, obj)).map((i) => obj[i]);
}

const findVendors = () => {
  const vendors = []
  // const assets = []
  // const vendors = { css: [], js: [], other: [] }
  const filenames = globule.find(src + '/**/*', 'Pages/**/*');

  filenames.forEach((filename) => {
    if (extension(filename) === '.html' || extension(filename) === '.cshtml') {
      const files = fs.readFileSync(filename, 'ascii').toString().split('\n')

      // go through the list of code lines
      files.forEach((file) => {

        // if the current line matches `/(?:href|src)="(node_modules.*.[css|js])"/`, it will be stored in the variable lines
        const nodeModules = file.match(/(?:href|src)="(\/vendors.*.[css|js])"/)
        if (nodeModules) {
          let vendor = []
          let src = (nodeModules[1]).replace("@@", "@").replace("/vendors", "node_modules")
		  
		  let foundAssembly = false;
		  var filepaths = globule.find('node_modules/' + src.split("/")[1] + '/**/*');
		  var jsFolderMatch = []
		  
		  filepaths.forEach((globFile) => {
				if (path.basename(src) == path.basename(globFile)){
					src = globFile;
					jsFolderMatch.push(globFile);
					foundAssembly = true;
				}
		  });

		  if (jsFolderMatch.length > 1)
		  {
			  jsFolderMatch.forEach((jsFileMatch) => {
					//Prevent usage of cjs/esm/umd folder for js files if there are any
					if (jsFileMatch.includes("/dist/")) {
					  src = jsFileMatch;
					}
					else if (jsFileMatch.includes("/js/"))
					{
						src = jsFileMatch;
					}
          else if (jsFileMatch.includes("/min/")) {
            src = jsFileMatch;
          }
			  });			  
		  }
		  
          const name = vendorName(src)
          let type
          let absolute

          vendor['name'] = name
          vendor['filetype'] = extension(src).replace('.', '')
          vendor['src'] = src
          vendor['absolute'] = resolve(src)

          if (vendors.findIndex(vendor => vendor.absolute === resolve(src)) === -1) {
            vendors.push(vendor)

            // Check it CSS file has assets
            if (extension(src) === '.css') {
              const assets = fs.readFileSync(resolve(src), 'ascii').toString().match(/(?:url)\((.*?)\)/ig)
              if (assets) {
                assets.forEach((asset) => {
                  const assetPath = asset.match(/(?:url)\((.*?)\)/)[1]
                  let subVendor = []
                  if (assetPath !== undefined) {
                    const path = assetPath.replace(/\?.*/, '').replace(/\#.*/, '').replace(/\'|\"/, '').replace(/\'|\"/, '')
                    subVendor['name'] = name
                    subVendor['filetype'] = 'other'
                    subVendor['src'] = normalize(`css/${path}`)
                    subVendor['absolute'] = resolve(dirname(src), path)

                    vendors.push(subVendor)
                  }
                })
              }
            }
          }
        }
      })
    }
  })
  return vendors
}

const copyFiles = (files, dest) => {
  files.forEach((file) => {
    let dir
    file.filetype !== 'other' ? dir = resolve(dest, file.name, file.filetype) : dir = resolve(dest, file.name, dirname(file.src))
    mkdirp.sync(dir)
    if (fs.lstatSync(file.absolute).isDirectory()) {
      console.log(`${file.absolute} is a directory, not a file - Ignore`)
      return;
    }

    // Copy Asserts - e.g. webfonts
    if (file.filetype !== "js" && file.filetype !== "css") {
      fs.createReadStream(file.absolute).pipe(fs.createWriteStream(resolve(dir, basename(file.src))))

      if (fs.existsSync(`${file.absolute}.map`)) {
        fs.createReadStream(`${file.absolute}.map`).pipe(fs.createWriteStream(resolve(dir, `${basename(file.src)}.map`)))
      }
    }

    // Minify JS
    if (file.filetype === "js") {
      let options = {
        mangle: {
          toplevel: true,
        },
        nameCache: {}
      };
      let outputFile = resolve(dir, basename(file.src))
      readFileAsync(file.absolute, { encoding: 'utf8' })
        .then((text) => {
          let minifiedJs = uglifyjs.minify(text, options);
          writeFileAsync(outputFile, minifiedJs.code)
            .catch((error) => {
              console.log(error)
            });
        })
        .catch((err) => {
          console.log('ERROR:', err);
        })
    }

    // Minify CSS
    if (file.filetype === "css") {
      let options = {
        level: {
          1: { specialComments: 0 }
        }
      }
      let outputFile = resolve(dir, basename(file.src))
      readFileAsync(file.absolute, { encoding: 'utf8' })
        .then((text) => {
          let minifiedCss = new cleancss(options).minify(text)
          writeFileAsync(outputFile, minifiedCss.styles)
            .catch((error) => {
              console.log(error)
            });
        })
        .catch((err) => {
          console.log('ERROR:', err);
        })
    }
  })
}

const replaceRecursively = (filename, vendor) => {
  const original = vendor.src
  const replacement = `vendors/${vendor.name}/${vendor.filetype}/${basename(vendor.src)}`
  sh.sed('-i', original, replacement, filename)
}

const main = () => {
  const vendors = findVendors()
  copyFiles(vendors.map((file) => { return file }), './' + dest + 'vendors/')
  /*const filenames = walkSync(dest)
  filenames.forEach((filename) => {
    if (extension(filename) === '.html') {
      vendors.map((vendor) => {
        if (vendor.filetype !== 'other') {
          replaceRecursively(resolve(filename), vendor)
        }
      })
    }
  })*/
}

main()
