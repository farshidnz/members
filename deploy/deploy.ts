#!/usr/bin/env node
import 'source-map-support/register';
import { DeployStack } from './lib/member-settings-stack';
import { getEnv, getResourceName } from '@cashrewards/cdk-lib'
import { App } from 'aws-cdk-lib';

const app = new App();
new DeployStack(app, getResourceName('MemberSettings'), {
  env: {
    account: getEnv("AWS_ACCOUNT_ID"),
    region: getEnv("AWS_REGION"),
  }  
});
