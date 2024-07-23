using Microsoft.Xrm.Sdk;
using System;

namespace GarageGroup.Internal.Timesheet.Plugin
{
    public sealed class LanguagePlugin : IPlugin
    {
        private const string PostImageName = "PostImage";

        private const string LanguageFieldName = "gg_language_code";

        private const string DefaultLanguageCode = "en";

        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                var crmServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                var pluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                var createdTelegramBotUser = pluginExecutionContext.PostEntityImages[PostImageName];

                if (string.IsNullOrWhiteSpace(createdTelegramBotUser.GetAttributeValue<string>(LanguageFieldName)) is false)
                {
                    return;
                }

                var service = crmServiceFactory.CreateOrganizationService(pluginExecutionContext.UserId);
                service.Update(new Entity()
                {
                    LogicalName = createdTelegramBotUser.LogicalName,
                    Id = createdTelegramBotUser.Id,
                    Attributes =
                    {
                        { LanguageFieldName, DefaultLanguageCode }
                    }
                });
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new InvalidPluginExecutionException($"An unexpected exception occured in '{nameof(LanguagePlugin)}'", exception);
            }
        }
    }
}
