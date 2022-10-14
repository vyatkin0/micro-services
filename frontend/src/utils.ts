import configApi from './configApi';

const apiUrl ="https://frontend-proxy-zavd5lj5qq-uc.a.run.app"; //return document.getElementsByTagName('base')[0].href;

export interface Tenant {
  id: number;
  name: string;
  company: string;
}

export interface LoginInfo {
  refreshToken: string;
  accessToken: string;
  isAdmin: boolean;
}

export interface UserInfo {
  id: number;
  firstName: string;
  lastName: string;
  company: string;
  email: string;
  name: string;
  tenants?: Tenant[];
}

let tenants: Tenant[] = [];
let user: UserInfo;

let accessToken: string | null = sessionStorage.getItem('accessToken');
export let isAdmin: boolean | null = sessionStorage.getItem('isAdmin') === 'true';

export function getTenants() {
  return tenants;
}

export function setTenants(userTenants: Tenant[]) {
  tenants = userTenants || [];
}


export function getUserInfo() {
  return user;
}

export function setUserInfo(u: UserInfo) {
  user = u;
  return user;
}

export function setLoginInfo(info: LoginInfo) {
  const { refreshToken: refresh, accessToken: access, isAdmin: loggedIsAdmin } = info;

  accessToken = access;
  isAdmin = loggedIsAdmin;

  sessionStorage.setItem('isAdmin', isAdmin.toString());
  sessionStorage.setItem('accessToken', access);
  localStorage.setItem('refreshToken', refresh);
}

export function removeRefreshToken() {
  isAdmin = null;
  accessToken = null;
  tenants = [];
  sessionStorage.removeItem('isAdmin');
  sessionStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
}

export function isValidAccessToken() {
  if (!accessToken) {
    return false;
  }

  const token = JSON.parse(atob(accessToken.split('.')[1]));
  return token.exp >= Date.now() / 1000 + 60;
}

export function rpc(service: string, iface: string, method: string, message = {}, auth = true) {

  const body: any = {
    service,
    interface: iface,
    method,
    message,
  };

  if (auth) {

    if (!isValidAccessToken()) {
      window.location.href = '/login';
      return;
    }

    body.headers = { authorization: accessToken };
  }

  return postFetch(body, '/rpc', false);
}

async function postFetch(body: object | null, apiPath: string, auth = true) {
  const request: RequestInit = {
    ...configApi.fetch,
    method: 'POST', // *GET, POST, PUT, DELETE, etc.
    headers: {
      'Content-Type': 'application/json',
    },
    body: body ? JSON.stringify(body) : undefined,
  };

  if (auth && accessToken) {

    let token = sessionStorage.getItem('accessToken');

    request.headers = {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer ' + token, //accessToken,
    }
  }

  const response = await fetch(apiUrl+apiPath, request);

  if (response.ok) {
    return response.json();
  } else {
    let err;
    const errText = await response.text();
    console.error(errText);
    try {
      err = JSON.parse(errText);
    } catch {
    }

    if (err && err.statusCode) {
      throw new Error(`Error ${err.statusCode}. ${err.detail}`);
    }

    throw new Error('Error: ' + errText);
  }
}
