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
  const make = new Function('imageLock','mounted','setPreparingImage','setError','setFile','optimizeImageForUpload','validateImageFile', `${body}; return chooseReference;`);
  const choose = make(imageLock,mounted, v=>updates.push(['busy',v]), v=>updates.push(['error',v]), v=>updates.push(['file',v]), ()=>process(),validateImageFile);
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

test('HTML Image backing URL remains alive through drawing and is released after processing', async () => {
  const originalCreate = URL.createObjectURL, originalRevoke = URL.revokeObjectURL;
  let revoked = false, draws = 0;
  URL.createObjectURL = () => 'blob:test-lifetime';
  URL.revokeObjectURL = () => { revoked = true; };
  globalThis.createImageBitmap = async () => { throw new Error('use HTML fallback'); };
  globalThis.Image = class {
    naturalWidth = 4000; naturalHeight = 3000;
    set src(value) { queueMicrotask(() => this.onload()); }
  };
  globalThis.document = {createElement: () => ({
    getContext: () => ({drawImage(){ assert.equal(revoked,false,'URL revoked before canvas draw'); draws++; }}),
    toBlob: callback => {assert.equal(revoked,false); callback(new Blob(['encoded'],{type:'image/webp'}));},
  })};
  try {
    const result = await optimizeImageForUpload(new File([jpeg],'camera.jpg',{type:'image/jpeg'}));
    assert.equal(result.type,'image/webp'); assert.equal(draws,1); assert.equal(revoked,true);
  } finally {URL.createObjectURL=originalCreate; URL.revokeObjectURL=originalRevoke;}
});

async function makeChooser(process) {
  const page = await readFile(new URL('../src/pages/CreateAiTattooPage.jsx',import.meta.url),'utf8');
  const body = page.slice(page.indexOf('  async function chooseReference'),page.indexOf('  const [file, setFile]'));
  const updates=[], imageLock={current:false}, mounted={current:true};
  const choose = new Function('imageLock','mounted','setPreparingImage','setError','setFile','optimizeImageForUpload','validateImageFile',`${body}; return chooseReference;`)(
    imageLock,mounted,v=>updates.push(['busy',v]),v=>updates.push(['error',v]),v=>updates.push(['file',v]),process,validateImageFile);
  return {choose,updates,imageLock,mounted};
}
for (const [name,bytes,type] of [['first valid selection',jpeg,'image/jpeg'],['first photo-library selection',png,'image/png'],['first camera-shaped selection',jpeg,'image/jpeg']]) {
  test(`${name} commits once, preserves input during processing, supports same-file reselection`,async()=>{
    decoder(); canvas();
    const state=await makeChooser(async file=>{assert.equal(input.value,'selected'); return optimizeImageForUpload(file);});
    const input={files:[new File([bytes],'photo',{type})],value:'selected'};
    await state.choose({currentTarget:input});
    assert.equal(state.updates.filter(([key])=>key==='file').length,1);
    assert.equal(input.value,''); assert.equal(state.imageLock.current,false);
    input.value='selected';
    await state.choose({currentTarget:input});
    assert.equal(state.updates.filter(([key])=>key==='file').length,2);
    await state.choose({currentTarget:{files:[],value:''}});
    assert.equal(state.updates.filter(([key])=>key==='file').length,2);
  });
}
test('valid optimization fallback reaches state on first attempt; corrupt file errors and unlocks',async()=>{
  decoder();canvas(true);
  const state=await makeChooser(file=>optimizeImageForUpload(file));
  const original=new File([jpeg],'valid.jpg',{type:'image/jpeg'});
  await state.choose({currentTarget:{files:[original],value:'selected'}});
  assert.deepEqual(state.updates.filter(([key])=>key==='file'),[['file',original]]);
  globalThis.createImageBitmap=async()=>{throw new Error('bad');};
  globalThis.Image=class {set src(value){queueMicrotask(()=>this.onerror());}};
  await state.choose({currentTarget:{files:[new File(['broken'],'bad.jpg',{type:'image/jpeg'})],value:'selected'}});
  assert.equal(state.updates.filter(([key])=>key==='file').length,1);
  assert.ok(state.updates.some(([key,value])=>key==='error' && value));
  assert.equal(state.imageLock.current,false);
  assert.deepEqual(state.updates.at(-1),['busy',false]);
});
test('empty processing result cannot silently leave the first selection empty',async()=>{
  const state=await makeChooser(async()=>undefined);
  await state.choose({currentTarget:{files:[new File([jpeg],'photo.jpg',{type:'image/jpeg'})],value:'selected'}});
  assert.equal(state.updates.filter(([key])=>key==='file').length,0);
  assert.ok(state.updates.some(([key,value])=>key==='error' && value==='Please select a valid image.'));
  assert.equal(state.imageLock.current,false);
  assert.deepEqual(state.updates.at(-1),['busy',false]);
});

test('WebKit PNG fallback ignores quality but dimension reduction fits limit on first selection',async()=>{
  let decodes=0, draws=0, encodes=0, closed=0;
  globalThis.createImageBitmap=async()=>{decodes++;return {width:4032,height:3024,close(){closed++;}};};
  globalThis.document={createElement:()=>({
    width:0,height:0,
    getContext:()=>({drawImage(){draws++;}}),
    toBlob(callback,type,quality){
      encodes++;
      // Model an unsupported WebP encoder returning lossless PNG; quality has no effect.
      const size=Math.ceil(this.width*this.height*4);
      callback(new Blob([new Uint8Array(size)],{type:'image/png'}));
    },
  })};
  const state=await makeChooser(file=>optimizeImageForUpload(file,{quality:0.88}));
  await state.choose({currentTarget:{files:[new File([jpeg],'iphone-photo.jpg',{type:'image/jpeg'})],value:'selected'}});
  const commits=state.updates.filter(([key])=>key==='file');
  assert.equal(commits.length,1);
  assert.ok(commits[0][1].size<=4.5*1024*1024);
  assert.equal(commits[0][1].type,'image/png');
  assert.match(commits[0][1].name,/\.png$/);
  assert.equal(decodes,1);assert.equal(closed,1);
  assert.ok(draws>1);assert.ok(encodes<=13);
  assert.equal(state.imageLock.current,false);
  assert.deepEqual(state.updates.at(-1),['busy',false]);
});

test('oversized lossy output also fits through bounded resizing, without decoding again',async()=>{
  let decodes=0;
  globalThis.createImageBitmap=async()=>{decodes++;return {width:4000,height:3000,close(){}};};
  globalThis.document={createElement:()=>({
    width:0,height:0,getContext:()=>({drawImage(){}}),
    toBlob(callback){callback(new Blob([new Uint8Array(Math.ceil(this.width*this.height*3))],{type:'image/webp'}));},
  })};
  const result=await optimizeImageForUpload(new File([jpeg],'large.jpg',{type:'image/jpeg'}));
  assert.ok(result.size<=4.5*1024*1024);assert.equal(decodes,1);
});
