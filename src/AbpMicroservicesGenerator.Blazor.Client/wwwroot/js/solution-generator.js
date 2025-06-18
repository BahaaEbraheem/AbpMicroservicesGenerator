window.downloadFile = (filename, base64Data) => {
    const link = document.createElement('a');
    link.href = 'data:application/zip;base64,' + base64Data;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

window.copyToClipboard = (text) => {
    navigator.clipboard.writeText(text).then(() => {
        console.log('Text copied to clipboard');
    }).catch(err => {
        console.error('Failed to copy text: ', err);
    });
};

window.showToast = (message, type = 'info') => {
    // Simple toast notification
    const toast = document.createElement('div');
    toast.className = `alert alert-${type} position-fixed`;
    toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    toast.innerHTML = `
        <div class="d-flex align-items-center">
            <span>${message}</span>
            <button type="button" class="btn-close ms-auto" onclick="this.parentElement.parentElement.remove()"></button>
        </div>
    `;
    
    document.body.appendChild(toast);
    
    // Auto remove after 5 seconds
    setTimeout(() => {
        if (toast.parentElement) {
            toast.remove();
        }
    }, 5000);
};

// Auto-suggest for solution names
window.generateSolutionName = (companyName) => {
    if (!companyName) return '';
    
    const cleanCompanyName = companyName.replace(/[^a-zA-Z0-9]/g, '');
    const suggestions = [
        `${cleanCompanyName}.Platform`,
        `${cleanCompanyName}.Services`,
        `${cleanCompanyName}.Core`,
        `${cleanCompanyName}.Enterprise`,
        `${cleanCompanyName}.Cloud`
    ];
    
    return suggestions[Math.floor(Math.random() * suggestions.length)];
};

// Port validation
window.validatePort = (port) => {
    return port >= 1000 && port <= 65535;
};

// Generate random available port
window.generateRandomPort = (usedPorts = []) => {
    let port;
    do {
        port = Math.floor(Math.random() * (65535 - 44300 + 1)) + 44300;
    } while (usedPorts.includes(port));
    
    return port;
};
