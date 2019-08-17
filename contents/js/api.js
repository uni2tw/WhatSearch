//axios.defaults.baseURL = 'http://localhost:7777/';
axios.defaults.baseURL = 'http://uni2.tw:7777/';
axios.defaults.headers.common['Authorization'] = '';
axios.defaults.headers.post['Content-Type'] = 'application/json; charset=utf-8';
axios.defaults.withCredentials = true;

var api = {};

api.folder = function (folder) {
    console.log('post /api/folder: ' + folder);
    return axios.post('/api/folder', {
        p: folder
    });
};

api.search = function(keyword) {
    console.log('post /api/search: ' + keyword);
    return axios.post('/api/search', {
        q: keyword
    });    
}
