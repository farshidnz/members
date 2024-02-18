import { App } from 'aws-cdk-lib';
import { Template } from 'aws-cdk-lib/assertions';
import * as Deploy from '../lib/member-settings-stack';

test('Empty Stack', () => {
    const app = new App();
    // WHEN
    const stack = new Deploy.DeployStack(app, 'MyTestStack');
    
    const template = Template.fromStack(stack);

    // THEN
    template.templateMatches({
      "Resources": {}
    })
});
