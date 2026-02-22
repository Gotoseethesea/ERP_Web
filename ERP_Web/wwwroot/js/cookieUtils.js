namespace ERP_Web.wwwroot.js
{
    // wwwroot/js/cookieUtils.js
    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        console.log("getCookie");
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    }

    function setCookie(name, value, days) {
        const date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        const expires = `expires=${date.toUTCString()}`;
        document.cookie = `${name}=${value};${expires};path=/;SameSite=Lax`;
    }

    function deleteCookie(name) {
        document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/;`;
    }

    // 导出函数到全局对象
    window.cookieHelper = {
        getCookie,
        setCookie,
        deleteCookie
    };
    // wwwroot/js/interop.js
    //window.getCookie = function (name) {
    //    const match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
    //    return match ? decodeURIComponent(match[2]) : null;
    //};

    //window.setCookie = function (name, value, days) {
    //    const date = new Date();
    //    date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
    //    document.cookie = `${name}=${encodeURIComponent(value)};expires=${date.toUTCString()};path=/;secure;samesite=lax`;
    //};

}
