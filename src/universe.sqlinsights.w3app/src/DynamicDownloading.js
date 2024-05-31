export const DynamicDownloading = (data, contentType = 'text/plain', fileName = 'download file name placeholder') => {
    const element = global.document.createElement('a');
    const file = new Blob([data], { type: contentType });
    element.href = global.URL.createObjectURL(file);
    element.download = fileName;
    element.click();
};
