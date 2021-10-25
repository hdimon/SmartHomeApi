//Trying to avoid any npm installs or anything that takes extra time...
const   https = require('https'),
        zlib = require('zlib'),
        fs = require('fs'),
        env = process.env;

function fail(message, exitCode=1) {
    console.log(`::error::${message}`);
    process.exit(1);
}

function request(method, path, data, callback) {
    
    try {
        if (data) {
            data = JSON.stringify(data);
        }  
        const options = {
            hostname: 'api.github.com',
            port: 443,
            path,
            method,
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': data ? data.length : 0,
                'Accept-Encoding' : 'gzip',
                'Authorization' : `token ${env.INPUT_TOKEN}`,
                'User-Agent' : 'GitHub Action - development'
            }
        }
        const req = https.request(options, res => {
    
            let chunks = [];
            res.on('data', d => chunks.push(d));
            res.on('end', () => {
                let buffer = Buffer.concat(chunks);
                if (res.headers['content-encoding'] === 'gzip') {
                    zlib.gunzip(buffer, (err, decoded) => {
                        if (err) {
                            callback(err);
                        } else {
                            callback(null, res.statusCode, decoded && JSON.parse(decoded));
                        }
                    });
                } else {
                    callback(null, res.statusCode, buffer.length > 0 ? JSON.parse(buffer) : null);
                }
            });
    
            req.on('error', err => callback(err));
        });
    
        if (data) {
            req.write(data);
        }
        req.end();
    } catch(err) {
        callback(err);
    }
}

function main() {
  const path = 'PRERELEASE_NUMBER/PRERELEASE_NUMBER';
        
  if (!env.INPUT_PREFIX) fail(`ERROR: Input parameter prefix is not defined.`);
  if (!env.INPUT_PRERELEASE_TYPE) fail(`ERROR: Input parameter prerelease_type is not defined.`);
        
  const prefix = `${env.INPUT_PREFIX}`;
  const prereleaseType = `${env.INPUT_PRERELEASE_TYPE}`;
        
  console.log(`Prefix: ${prefix}`);
  console.log(`Prerelease type: ${prereleaseType}`);
        
  //See if we've already generated the build number and are in later steps...
  if (fs.existsSync(path)) {
      let prereleaseNumber = fs.readFileSync(path);
      console.log(`Prerelease number already generated in earlier jobs, using prerelease number ${prereleaseNumber}...`);
      //Setting the output and a environment variable to new prerelease number...
      fs.writeFileSync(process.env.GITHUB_ENV, `PRERELEASE_NUMBER=${prereleaseNumber}`);
      console.log(`::set-output name=prerelease_number::${prereleaseNumber}`);
      return;
  }
        
  //Some sanity checking:
  for (let varName of ['INPUT_TOKEN', 'GITHUB_REPOSITORY', 'GITHUB_SHA']) {
      if (!env[varName]) {
          fail(`ERROR: Environment variable ${varName} is not defined.`);
      }
  }

  request('GET', `/repos/${env.GITHUB_REPOSITORY}/git/refs/tags/${prefix}-${prereleaseType}-`, null, (err, status, result) => {
      let nextPrereleaseNumber, nrTags;
          
      if (status === 404) {
          console.log('No prerelease-number ref available, starting at 1.');
          nextPrereleaseNumber = 1;
          nrTags = [];
      } else if (status === 200) {
            const regexString = `/${prefix}-${prereleaseType}-(\\d+)$`;
            const regex = new RegExp(regexString);
            nrTags = result.filter(d => d.ref.match(regex));
              
            /*console.log(`Prefix tags:`);
            for (let i = 0; i < nrTags.length; i++) {
                console.log(`Tag: ${JSON.stringify(nrTags[i])}`);
            }*/
              
            //Check if commit already has tagm than no need to update it.
            const tag = nrTags.find((t) => t.object.sha === env.GITHUB_SHA);
              
            if (tag) {
                const existingNumber = parseInt(tag.ref.match(/-(\d+)$/)[1]);
                console.log(`Prerelease number already generated in earlier jobs, using prerelease number ${existingNumber}...`);
                //Setting the output and a environment variable to existing prerelease number...
                fs.writeFileSync(process.env.GITHUB_ENV, `PRERELEASE_NUMBER=${existingNumber}`);
                console.log(`::set-output name=prerelease_number::${existingNumber}`);
                return;
            }            
            
            //Existing prerelease numbers:
            let nrs = nrTags.map(t => parseInt(t.ref.match(/-(\d+)$/)[1]));
              
            //console.log(`Prerelease numbers: ${nrs.join(', ')}`);
    
            let currentPrereleaseNumber = Math.max(...nrs);
            console.log(`Last prerelease number for ${prereleaseType} was ${currentPrereleaseNumber}.`);
    
            nextPrereleaseNumber = currentPrereleaseNumber + 1;
            console.log(`Updating prerelease counter to ${nextPrereleaseNumber}...`);
        } else {
            if (err) {
                fail(`Failed to get refs. Error: ${err}, status: ${status}`);
            } else {
                fail(`Getting prerelease-number refs failed with http status ${status}, error: ${JSON.stringify(result)}`);
            } 
        }
          
        let newRefData = {
            ref:`refs/tags/${prefix}-${prereleaseType}-${nextPrereleaseNumber}`, 
            sha: env.GITHUB_SHA
        };
          
        request('POST', `/repos/${env.GITHUB_REPOSITORY}/git/refs`, newRefData, (err, status, result) => {
            if (status !== 201 || err) {
                fail(`Failed to create new prerelease-number ref. Status: ${status}, err: ${err}, result: ${JSON.stringify(result)}`);
            }

            console.log(`Successfully updated prerelease number to ${nextPrereleaseNumber}`);
                
            //Setting the output and a environment variable to new prerelease number...
            fs.writeFileSync(process.env.GITHUB_ENV, `PRERELEASE_NUMBER=${nextPrereleaseNumber}`);
                
            console.log(`::set-output name=prerelease_number::${nextPrereleaseNumber}`);
            //Save to file so it can be used for next jobs...
            fs.writeFileSync('PRERELEASE_NUMBER', nextPrereleaseNumber.toString());
        });
  });
}

main();
