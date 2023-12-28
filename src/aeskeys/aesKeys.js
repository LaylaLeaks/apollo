const { Client, Language } = require('fnapicom');
const fs = require('fs');
const path = require('path');

const client = new Client({
  language: Language.English,
});

client.aesKeys()
  .then(response => {
    if (response.status === 200) {
      const keys = response.data;
      const currentDate = new Date().toLocaleDateString().replace(/\//g, '-');
      const content = `${JSON.stringify(keys, null, 2)}`;

      const folderPath = path.join(__dirname, '../../export/aeskeys');
      if (!fs.existsSync(folderPath)) {
        fs.mkdirSync(folderPath);
      }

      const filename = `AesKeys_${currentDate}.json`;
      const filePath = path.join(folderPath, filename);
      fs.writeFileSync(filePath, content);
      console.log('Here all currently AesKeys:', keys);
    } else {
      console.error(response.status);
    }
  })
  .catch(error => {
    console.error('Error when retrieving the AES keys:', error);
});