/**
 * Configuration for fetching backend methods
 */

declare let process: any;
const isDevMode = true;//!process.env.NODE_ENV || process.env.NODE_ENV === 'development';

export default class {
    static fetch: RequestInit = {
        mode: isDevMode ? 'cors' : 'same-origin', // no-cors, *cors, same-origin
        cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
        credentials: !isDevMode ? 'same-origin' : 'include', // include, *same-origin, omit
        redirect: 'follow', // manual, *follow, error
        referrerPolicy: 'no-referrer', // no-referrer, *no-referrer-when-downgrade, origin, origin-when-cross-origin, same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url
    };
}
