import fs from 'node:fs';
import path from 'node:path';
import ts from 'typescript';

const root = path.resolve(import.meta.dirname, '..');
const localeRoot = path.join(root, 'src', 'i18n', 'local');

const walk = (directory, extension) => fs.readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
  const target = path.join(directory, entry.name);
  return entry.isDirectory() ? walk(target, extension) : entry.name.endsWith(extension) ? [target] : [];
});

const literalValue = (node) => ts.isStringLiteralLike(node) || ts.isNoSubstitutionTemplateLiteral(node)
  ? node.text
  : undefined;

const loadLocale = (language) => {
  const values = new Map();
  const origins = new Map();
  const errors = [];
  for (const file of walk(path.join(localeRoot, language), '.ts')) {
    const source = ts.createSourceFile(file, fs.readFileSync(file, 'utf8'), ts.ScriptTarget.Latest, true, ts.ScriptKind.TS);
    const exported = source.statements.find((statement) => ts.isExportAssignment(statement));
    if (!exported || !ts.isObjectLiteralExpression(exported.expression)) {
      errors.push(`${path.relative(root, file)} does not export a translation object.`);
      continue;
    }
    for (const property of exported.expression.properties) {
      if (!ts.isPropertyAssignment(property)) continue;
      const key = ts.isStringLiteralLike(property.name) ? property.name.text : undefined;
      const value = literalValue(property.initializer);
      if (!key || value === undefined) continue;
      if (values.has(key)) errors.push(`Duplicate ${language} key "${key}" in ${path.relative(root, file)} and ${origins.get(key)}.`);
      values.set(key, value);
      origins.set(key, path.relative(root, file));
    }
  }
  return { values, errors };
};

const variables = (value) => Array.from(value.matchAll(/\{\{\s*([\w.-]+)(?:\s*,[^}]*)?\s*\}\}/g), (match) => match[1]).sort();
const fr = loadLocale('fr');
const en = loadLocale('en');
const errors = [...fr.errors, ...en.errors];

for (const key of fr.values.keys()) if (!en.values.has(key)) errors.push(`Missing English translation for "${key}".`);
for (const key of en.values.keys()) if (!fr.values.has(key)) errors.push(`Missing French translation for "${key}".`);
for (const [key, valueFr] of fr.values) {
  const valueEn = en.values.get(key);
  if (valueEn === undefined) continue;
  const variablesFr = variables(valueFr);
  const variablesEn = variables(valueEn);
  if (variablesFr.join('|') !== variablesEn.join('|')) {
    errors.push(`Interpolation variables differ for "${key}": FR [${variablesFr}] / EN [${variablesEn}].`);
  }
}

const knownKeys = new Set([...fr.values.keys(), ...en.values.keys()]);
for (const file of walk(path.join(root, 'src'), '.ts').concat(walk(path.join(root, 'src'), '.tsx'))) {
  if (file.startsWith(localeRoot)) continue;
  const source = ts.createSourceFile(file, fs.readFileSync(file, 'utf8'), ts.ScriptTarget.Latest, true, file.endsWith('.tsx') ? ts.ScriptKind.TSX : ts.ScriptKind.TS);
  const visit = (node) => {
    if (ts.isCallExpression(node) && ts.isIdentifier(node.expression) && node.expression.text === 't') {
      const argument = node.arguments[0];
      const hasKey = (key) => knownKeys.has(key) || knownKeys.has(`${key}_one`) || knownKeys.has(`${key}_other`);
      if (argument && ts.isStringLiteralLike(argument) && /^(admin|public)\./.test(argument.text) && !hasKey(argument.text)) {
        const position = source.getLineAndCharacterOfPosition(argument.getStart(source));
        errors.push(`Undefined translation key "${argument.text}" at ${path.relative(root, file)}:${position.line + 1}.`);
      }
    }
    ts.forEachChild(node, visit);
  };
  visit(source);
}

if (errors.length > 0) {
  console.error(`Bilingual integrity check failed with ${errors.length} issue(s):`);
  for (const error of errors) console.error(`- ${error}`);
  process.exit(1);
}

console.log(`Bilingual integrity check passed: ${fr.values.size} matched FR/EN keys with compatible variables.`);
