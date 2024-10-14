const set_document_title = (title) => { document.title = title };
const log = (text) => { console.log(text) };
const create_UI = (node) => { document.body.appendChild(node) };
const txt = (text) => { return document.createTextNode(text); };
const div = (children) => { const elem = document.createElement('div');
	for (const child of children) {
		elem.appendChild(child);
	}
	return elem; };
const button = (children) => { const elem = document.createElement('button');
	for (const child of children) {
		elem.appendChild(child);
	}
	return elem; };
set_document_title("Next Gen To-Do App")
create_UI(div([div([txt("0")]), div([button([txt("Click me!")])])]))
