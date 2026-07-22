const { execSync } = require('child_process');
try {
  const result = execSync('npx tsc --noEmit', { cwd: 'c:\\\\Univva\\\\Dairyprod\\\\Grocery-Service\\\\Grocery.Frontend', encoding: 'utf-8' });
  console.log('SUCCESS:', result);
} catch (e) {
  console.log('ERROR:', e.stdout);
}
