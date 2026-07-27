const bulgarianToLatin = {
  а: "a", б: "b", в: "v", г: "g", д: "d", е: "e", ж: "zh",
  з: "z", и: "i", й: "y", к: "k", л: "l", м: "m", н: "n",
  о: "o", п: "p", р: "r", с: "s", т: "t", у: "u", ф: "f",
  х: "h", ц: "ts", ч: "ch", ш: "sh", щ: "sht", ъ: "a",
  ь: "y", ю: "yu", я: "ya",
};

const cityAliases = [
  ["София", "Sofia"],
  ["Пловдив", "Plovdiv"],
  ["Варна", "Varna"],
  ["Бургас", "Burgas"],
  ["Русе", "Ruse"],
  ["Стара Загора", "Stara Zagora"],
  ["Плевен", "Pleven"],
  ["Сливен", "Sliven"],
  ["Добрич", "Dobrich"],
  ["Шумен", "Shumen"],
  ["Перник", "Pernik"],
  ["Хасково", "Haskovo"],
  ["Ямбол", "Yambol"],
  ["Пазарджик", "Pazardzhik"],
  ["Благоевград", "Blagoevgrad"],
  ["Велико Търново", "Veliko Tarnovo"],
  ["Враца", "Vratsa"],
  ["Габрово", "Gabrovo"],
  ["Асеновград", "Asenovgrad"],
  ["Казанлък", "Kazanlak"],
];

function preserveCase(source, replacement) {
  if (source === source.toUpperCase()) return replacement.toUpperCase();
  if (source[0] === source[0]?.toUpperCase()) {
    return replacement[0].toUpperCase() + replacement.slice(1);
  }
  return replacement;
}

export function transliterateBulgarian(value) {
  return Array.from(value).map((character) => {
    const lower = character.toLocaleLowerCase("bg-BG");
    const replacement = bulgarianToLatin[lower];
    return replacement ? preserveCase(character, replacement) : character;
  }).join("");
}

export function getSearchAliases(value) {
  const query = value.trim();
  if (!query) return [""];

  const aliases = new Set([query]);
  const transliterated = transliterateBulgarian(query);
  if (transliterated !== query) aliases.add(transliterated);

  cityAliases.forEach(([bulgarian, latin]) => {
    const bulgarianPattern = new RegExp(bulgarian, "giu");
    const latinPattern = new RegExp(latin, "giu");

    if (bulgarianPattern.test(query)) {
      aliases.add(query.replace(bulgarianPattern, latin));
    }

    if (latinPattern.test(query)) {
      aliases.add(query.replace(latinPattern, bulgarian));
    }
  });

  return Array.from(aliases);
}
