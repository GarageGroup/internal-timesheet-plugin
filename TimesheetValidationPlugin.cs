using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace GarageGroup.Internal.Timesheet.Plugin
{
    public sealed class TimesheetValidationPlugin : IPlugin
    {
        private const string PreImageName = "PreImage";

        private const string StateCodeFieldName = "statecode";

        private const string DateFieldName = "gg_date";

        private const string PeriodEntityName = "gg_employee_cost_period";

        private const string PeriodFromDateFieldName = "gg_from_date";

        private const string PeriodToDateFieldName = "gg_to_date";

        public void Execute(IServiceProvider serviceProvider)
        {
            var crmServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var pluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var logger = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var service = crmServiceFactory.CreateOrganizationService(pluginExecutionContext.UserId);
            
            var operationName = pluginExecutionContext.MessageName.ToLower();
            logger.Trace($"Operation name: {operationName}");
            logger.Trace($"Operation id: {pluginExecutionContext.OperationId}");

            var preEntity = pluginExecutionContext.PreEntityImages.GetDataOrNull(PreImageName);
            if (preEntity?.TryGetAttributeValue<OptionSetValue>(StateCodeFieldName, out var stateCode) is true && stateCode?.Value != 0)
            {
                logger.Trace($"State code: {stateCode?.Value}");

                throw new InvalidPluginExecutionException($"Cannot {operationName} the timesheet entry: the record is inactive.");
            }

            var target = pluginExecutionContext.InputParameters.GetDataOrNull("Target") as Entity;
            if (target?.TryGetAttributeValue<DateTime>(DateFieldName, out var date) is true)
            {
                logger.Trace($"Date: {date}");

                if (DateTime.UtcNow < date)
                {
                    throw new InvalidPluginExecutionException($"Cannot {operationName} a timesheet entry: the timesheet date cannot be in the future.");
                }

                var periodCollection = service.RetrieveMultiple(BuildPeriodQuery(date));
                if (periodCollection.Entities.Count is 0)
                {
                    throw new InvalidPluginExecutionException($"Cannot {operationName} a timesheet entry: the timesheet date does not fall within an active billing period.");
                }
            }
        }

        private static QueryExpression BuildPeriodQuery(DateTime date)
            =>
            new QueryExpression
            {
                EntityName = PeriodEntityName,
                ColumnSet = new ColumnSet("gg_employee_cost_periodid"),
                Criteria =
                {
                    Filters =
                    {
                        new FilterExpression
                        {
                            FilterOperator = LogicalOperator.And,
                            Conditions =
                            {
                                new ConditionExpression(PeriodFromDateFieldName, ConditionOperator.LessEqual, date),
                                new ConditionExpression(PeriodToDateFieldName, ConditionOperator.GreaterEqual, date),
                                new ConditionExpression(StateCodeFieldName, ConditionOperator.Equal, 0)
                            }
                        }
                    }
                }
            };
    }
}
