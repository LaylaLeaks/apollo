const fs = require('fs');
const { Client, Language } = require('fnapicom');
const path = require('path');

const client = new Client({
  language: Language.English
});

const currentDate = new Date();
const formattedDate = currentDate.toISOString().split('T')[0];
const outputFileName = `cosmetics_${formattedDate}.json`;
const outputDirectory = 'export/cosmetics/all'; // Passe den Pfad an
const outputFilePath = path.join(outputDirectory, outputFileName);

console.log('Path:', outputFilePath);

// Überprüfe, ob das Verzeichnis existiert, und erstelle es gegebenenfalls
if (!fs.existsSync(outputDirectory)) {
  fs.mkdirSync(outputDirectory, { recursive: true });
  console.log('Path created:', outputDirectory);
}

client.cosmeticsList()
  .then(response => {
    // Ausgabe der vollständigen Antwort
    console.log('API response:', response);

    // Überprüfen, ob die erwarteten Daten vorhanden sind
    if (!response || !response.data || !Array.isArray(response.data)) {
      console.error('Unknown format. Expected an array.');
      return;
    }

    // Nur die gewünschten Felder extrahieren
    const simplifiedCosmetics = response.data.map(({ id, name, description, itemPreviewHeroPath, path, added }) => ({
      id,
      name,
      description,
      itemPreviewHeroPath,
      path,
      added
    }));

    // Füge das aktuelle Datum und die Uhrzeit hinzu
    const dataWithDate = {
      date: formattedDate,
      cosmetics: simplifiedCosmetics
    };

    console.log(dataWithDate);

    // Füge eine kurze Verzögerung hinzu (z.B. 500 ms), bevor du die Daten in die JSON-Datei schreibst
    setTimeout(() => {
      // Schreibe die Daten in eine JSON-Datei mit dem aktuellen Datum im Namen
      fs.writeFile(outputFilePath, JSON.stringify(dataWithDate, null, 2), (err) => {
        if (err) {
          console.error('Error when writing the JSON file:', err);
        } else {
          console.log('Data successfully written to the JSON file:', outputFilePath);
        }
      });
    }, 500);
  })
  .catch(error => {
    console.error('An error occurred while searching for all cosmetics:', error);
  });
