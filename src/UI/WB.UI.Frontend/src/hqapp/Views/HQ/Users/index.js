import Headquarters from './Headquarters'
import Manage from './Manage'
import Observers from './Observers'
import ApiUsers from './ApiUsers'
import Create from './Create'
import Supervisors from './Supervisors'
import Interviewers from './Interviewers'
import TwoFactorAuthentication from './TwoFactorAuthentication'
import SetupAuthenticator from './SetupAuthenticator'
import ResetAuthenticator from './ResetAuthenticator'
import ShowRecoveryCodes from './ShowRecoveryCodes'
import GenerateRecoveryCodes from './GenerateRecoveryCodes'
import Disable2fa from './Disable2fa'
import ChangePassword from './ChangePassword'

export default class UsersComponent {
    constructor(rootStore) {
        this.rootStore = rootStore
    }

    get routes() {
        return [{
            path: '/Headquarters', component: Headquarters,
        },
        {
            path: '/Observers', component: Observers,
        },
        {
            path: '/Interviewers', component: Interviewers,
        },
        {
            path: '/ApiUsers', component: ApiUsers,
        },
        {
            path: '/Supervisors', component: Supervisors,
        },
        {
            path: '/Users/Manage/:userId', component: Manage,
        },
        {
            path: '/Users/Manage/', component: Manage,
        },
        {
            path: '/Users/Create/:role', component: Create,
        },
        {
            path: '/Users/TwoFactorAuthentication', component: TwoFactorAuthentication,
        },
        {
            path: '/Users/TwoFactorAuthentication/:userId', component: TwoFactorAuthentication,
        },
        {
            path: '/Users/SetupAuthenticator', component: SetupAuthenticator,
        },
        {
            path: '/Users/SetupAuthenticator/:userId', component: SetupAuthenticator,
        },
        {
            path: '/Users/ResetAuthenticator', component: ResetAuthenticator,
        },
        {
            path: '/Users/ResetAuthenticator/:userId', component: ResetAuthenticator,
        },
        {
            path: '/Users/ShowRecoveryCodes', component: ShowRecoveryCodes,
        },
        {
            path: '/Users/ShowRecoveryCodes/:userId', component: ShowRecoveryCodes,
        },
        {
            path: '/Users/GenerateRecoveryCodes', component: GenerateRecoveryCodes,
        },
        {
            path: '/Users/GenerateRecoveryCodes/:userId', component: GenerateRecoveryCodes,
        },
        {
            path: '/Users/Disable2fa', component: Disable2fa,
        },
        {
            path: '/Users/Disable2fa/:userId', component: Disable2fa,
        },
        {
            path: '/Users/ChangePassword/:userId', component: ChangePassword,
        },
        {
            path: '/Users/ChangePassword/', component: ChangePassword,
        }]
    }
}
