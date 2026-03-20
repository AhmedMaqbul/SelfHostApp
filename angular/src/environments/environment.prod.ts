import { Environment } from '@abp/ng.core';

const baseUrl = 'https://localhost:44321';

const oAuthConfig = {
  issuer: baseUrl + '/',
  redirectUri: baseUrl + '/app',
  clientId: 'SelfHostApp_App',
  responseType: 'code',
  scope: 'offline_access SelfHostApp',
  requireHttps: true,
};

export const environment = {
  production: true,
  application: {
    baseUrl,
    name: 'SelfHostApp',
  },
  oAuthConfig,
  apis: {
    default: {
      url: baseUrl,
      rootNamespace: 'SelfHostApp',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
} as Environment;
