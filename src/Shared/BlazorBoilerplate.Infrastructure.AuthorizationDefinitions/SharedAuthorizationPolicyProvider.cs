﻿using BlazorBoilerplate.Constants;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BlazorBoilerplate.Infrastructure.AuthorizationDefinitions
{
    public class SharedAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        private readonly AuthorizationOptions _options;

        public SharedAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
            _options = options.Value;
        }

        public override async Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            var policy = await base.GetPolicyAsync(policyName);

            if (policy == null)
            {
                bool created = false;
                switch (policyName)
                {
                    //In DatabaseInitializer: await _userManager.AddClaimAsync(applicationUser, new Claim($"Is{role}", ClaimValues.trueString));
                    case Policies.IsAdmin:
                        policy = new AuthorizationPolicyBuilder()
                            .Combine(await GetPolicyAsync(Policies.IsUser))
                            .RequireClaim("IsAdministrator")
                            .Build();

                        created = true;
                        break;

                    case Policies.IsUser:
                        policy = new AuthorizationPolicyBuilder()
                            .RequireAuthenticatedUser()
                            .AddRequirements(new EmailVerifiedRequirement(true))
                            #if !DEBUG
                            .RequireClaim(JwtClaimTypes.AuthenticationMethod, ClaimValues.AuthenticationMethodMFA)
                            #endif
                            .RequireClaim(ApplicationClaimTypes.IsSubscriptionActive, ClaimValues.trueString)
                            .Build();

                        created = true;
                        break;

                    //https://docs.microsoft.com/it-it/aspnet/core/security/authentication/mfa
                    case Policies.TwoFactorEnabled:
                        policy = new AuthorizationPolicyBuilder()
                            .RequireAuthenticatedUser()
                            .RequireClaim(JwtClaimTypes.AuthenticationMethod, ClaimValues.AuthenticationMethodMFA)
                            .Build();

                        created = true;
                        break;

                    case Policies.IsSubscriptionActive:
                        policy = new AuthorizationPolicyBuilder()
                            .RequireAuthenticatedUser()
                            .RequireClaim(ApplicationClaimTypes.IsSubscriptionActive, ClaimValues.trueString)
                            .Build();

                        created = true;
                        break;

                    case Policies.IsMyEmailDomain:
                        policy = new AuthorizationPolicyBuilder()
                            .RequireAuthenticatedUser()
                            .AddRequirements(new DomainRequirement("blazorBoilerplate.com"))
                            .Build();

                        created = true;
                        break;

                    default:
                        if (Enum.TryParse(policyName.Replace("Is", string.Empty), out UserFeatures userFeature))
                        {
                            switch (userFeature)
                            {

                                case UserFeatures.Operator:
                                    policy = new AuthorizationPolicyBuilder()
                                        .Combine(await GetPolicyAsync(Policies.IsUser))
                                        .RequireAssertion(ctx =>
                                        ctx.User.IsInRole(DefaultRoleNames.Administrator) ||
                                        ctx.User.HasClaim(claim => claim.Type == ApplicationClaimTypes.For(UserFeatures.Operator) && claim.Value == ClaimValues.trueString))
                                        .Build();

                                    created = true;
                                    break;

                            }
                        }
                        break;
                }

                if (created)
                    _options.AddPolicy(policyName, policy);
            }

            return policy;
        }
    }
}
