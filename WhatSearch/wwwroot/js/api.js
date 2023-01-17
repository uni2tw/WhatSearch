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

api.breadcrumbs = function(folder) {
    console.log('post /api/breadcrumbs: ' + folder);
    return axios.post('/api/breadcrumbs', {
        p: folder
    });    
}


api.search = function(keyword) {
    console.log('post /api/search: ' + keyword);
    return axios.post('/api/search', {
        q: keyword
    });
}

api.pathId = function(pathname) {
    console.log('post /api/pathId: ' + pathname);
    return axios.post('/api/pathId', {
        pathname: pathname
    });
}