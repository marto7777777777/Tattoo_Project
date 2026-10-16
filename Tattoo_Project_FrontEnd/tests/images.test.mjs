import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
const source = (await readFile(new URL('../src/utils/images.js', import.meta.url), 'utf8'))
  .replace('import { API_BASE_URL } from "../api/apiConfig";', 'const API_BASE_URL = "https://api.inkroute.app";');
const moduleUrl = `data:text/javascript;base64,${Buffer.from(source).toString('base64')}`;
const { validateImageFile, optimizeImageForUpload, optimizeImagesForUpload } = await import(moduleUrl);
const jpeg = new Uint8Array([255,216,255,224,0,0,0,0,0,0,0,0]);
const png = new Uint8Array([137,80,78,71,13,10,26,10,0,0,0,0]);
function decoder(width = 20, height = 20) {
  globalThis.createImageBitmap = async () => ({width, height, close(){},});
}
function canvas(fail = false, type = 'image/webp') {
  globalThis.document = {createElement: () => ({
    getContext: () => ({drawImage(){}}),
    toBlob: (callback) => callback(fail ? null : new Blob(['encoded'], {type})),
  })};
}
test('basic validation accepts empty MIME but rejects nonimage MIME and empty file; no invented input cap', () => {
  for (const file of [null, new Blob([]), new Blob(['svg'], {type:'image/svg+xml'})]) assert.throws(() => validateImageFile(file));
  validateImageFile(new Blob(['unknown']));
  const large = new Blob(['x'], {type:'image/jpeg'});
  Object.defineProperty(large, 'size', {value:31 * 1024 * 1024});
  validateImageFile(large);
});
test('small corrupt file with JPEG MIME or empty MIME cannot bypass failed decode', async () => {
  globalThis.createImageBitmap = async () => { throw new Error('decode'); };
  globalThis.Image = class { set src(value) { queueMicrotask(() => this.onerror()); } };
  for (const type of ['image/jpeg', '']) await assert.rejects(optimizeImageForUpload(new Blob(['corrupt'], {type})), /decoded/);
});
test('JPEG, PNG and empty MIME decodable images optimize successfully with real output MIME', async () => {
  decoder(); canvas(false, 'image/png');
  for (const [bytes, type] of [[jpeg,'image/jpeg'],[png,'image/png'],[png,'']]) {
    const result = await optimizeImageForUpload(new File([bytes], 'photo', {type}));
    assert.equal(result.type, 'image/png');
    assert.match(result.name, /\.png$/);
  }
});
test('optimization failure after decode allows supported signature checked small originals', async () => {
  decoder(); canvas(true);
  for (const [bytes, type] of [[jpeg,'image/jpeg'],[png,'image/png']]) {
    const file = new File([bytes], 'photo', {type});
    assert.equal(await optimizeImageForUpload(file), file);
  }
  const result = await optimizeImageForUpload(new File([png], 'unknown', {type:''}));
  assert.equal(result.type, 'image/png');
  assert.deepEqual(new Uint8Array(await result.arrayBuffer()), png);
  for (const file of [new Blob(['not jpeg'], {type:'image/jpeg'}), new Blob([png], {type:'image/jpeg'})]) {
    await assert.rejects(optimizeImageForUpload(file));
  }
});
test('fallback cannot bypass byte/dimension limits, and oversized output rejects', async () => {
  decoder(); canvas(true);
  await assert.rejects(optimizeImageForUpload(new File([jpeg], 'x.jpg', {type:'image/jpeg'}), {maxBytes: 1}));
  decoder(4096,4096);
  await assert.rejects(optimizeImageForUpload(new File([jpeg], 'x.jpg', {type:'image/jpeg'})));
  canvas();
  await assert.rejects(optimizeImageForUpload(new File([jpeg], 'x.jpg', {type:'image/jpeg'}), {maxBytes: 1}), /too large/);
});
test('WebP preserved, decode resource released, combined limit enforced', async () => {
  let closed = 0;
  globalThis.createImageBitmap = async () => ({width:20, height:20, close(){closed++;}});
  const image = new File(['RIFF0000WEBP'], 'valid.webp', {type:'image/webp'});
  assert.equal(await optimizeImageForUpload(image), image);
  assert.equal(closed, 1);
  await assert.rejects(optimizeImagesForUpload([image,image], {maxTotalBytes: 4}), /too large together/);
});
test('both AI requests append exactly one unchanged prepared reference without decoding again', async () => {
  const api = (await readFile(new URL('../src/api/aiTattooApi.js', import.meta.url), 'utf8'))
    .replace('import { apiRequest, requestJson } from "./http";', 'const apiRequest = globalThis.testApiRequest; const requestJson = () => {};')
    .replace('"../utils/images"', JSON.stringify(moduleUrl));
  let calls = 0;
  const file = new File([png], 'ready.png', {type:'image/png'});
  globalThis.testApiRequest = async (url, options) => {
    calls++;
    assert.equal(options.body.getAll('referenceImage').length, 1);
    assert.equal(options.body.get('referenceImage'), file);
    assert.deepEqual([...options.body.keys()], ['title','tattooStyle','placement','description','referenceImage']);
    return {ok:true, json: async () => ({id:1})};
  };
  globalThis.createImageBitmap = () => { throw new Error('second decode forbidden'); };
  const { createFreeAiProject, createPaidAiDraft } = await import(`data:text/javascript;base64,${Buffer.from(api).toString('base64')}`);
  const values = {title:'x', tattooStyle:'x', placement:'x', description:'x'};
  await createFreeAiProject(values,file); await createPaidAiDraft(values,file);
  assert.equal(calls, 2);
  delete globalThis.testApiRequest;
});
test('AI chooser cancel preserves state, errors unlock and unmount prevents state updates', async () => {
  const page = await readFile(new URL('../src/pages/CreateAiTattooPage.jsx', import.meta.url), 'utf8');
  const body = page.slice(page.indexOf('  async function chooseReference'), page.indexOf('  const [file, setFile]'));
  let updates = [], process;
  const imageLock = {current:false}, mounted = {current:true};
  const make = new Function('imageLock','mounted','setPreparingImage','setError','setFile','optimizeImageForUpload', `${body}; return chooseReference;`);
  const choose = make(imageLock,mounted, v=>updates.push(['busy',v]), v=>updates.push(['error',v]), v=>updates.push(['file',v]), ()=>process());
  await choose({currentTarget:{files:[],value:'old'}});
  assert.deepEqual(updates, []);
  process = async () => {throw new Error('bad');};
  await choose({currentTarget:{files:[new Blob(['x'])],value:''}});
  assert.equal(imageLock.current,false);
  assert.deepEqual(updates.at(-1), ['busy',false]);
  updates = [];
  let complete;
  process = () => new Promise(resolve => {complete=resolve;});
  const pending = choose({currentTarget:{files:[new Blob(['x'])],value:''}});
  const before = updates.length;
  await choose({currentTarget:{files:[new Blob(['x'])],value:''}});
  assert.equal(updates.length,before);
  mounted.current = false; complete(new Blob(['prepared'])); await pending;
  assert.equal(updates.length,before);
  assert.equal(imageLock.current,false);
});
test('existing profile/studio/portfolio/request/moderation settings retain valid fallback and output limits', async () => {
  for (const maxWidth of [1200,2048]) {
    decoder(900,900); canvas(true);
    const original = new File([jpeg], 'photo.jpg', {type:'image/jpeg'});
    assert.equal(await optimizeImageForUpload(original, {maxWidth,maxHeight:maxWidth,quality:0.86}), original);
    decoder(3000,3000); canvas();
    const optimized = await optimizeImageForUpload(original, {maxWidth,maxHeight:maxWidth,quality:0.86});
    assert.equal(optimized.type,'image/webp');
    assert.ok(optimized.size <= 4.5 * 1024 * 1024);
  }
});
