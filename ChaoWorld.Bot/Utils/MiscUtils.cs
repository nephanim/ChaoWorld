using System;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

using Myriad.Rest.Exceptions;

using Newtonsoft.Json;

using Npgsql;

using ChaoWorld.Core;

using Polly.Timeout;

namespace ChaoWorld.Bot
{
    public static class MiscUtils
    {

        public static bool IsOurProblem(this Exception e)
        {
            // This function filters out sporadic errors out of our control from being reported to Sentry
            // otherwise we'd blow out our error reporting budget as soon as Discord takes a dump, or something.

            // Occasionally Discord's API will Have A Bad Time and return a bunch of CloudFlare errors (in HTML format).
            // The library tries to parse these HTML responses as JSON and crashes with a consistent exception message.
            if (e is JsonReaderException jre && jre.Message == "Unexpected character encountered while parsing value: <. Path '', line 0, position 0.") return false;

            // And now (2020-05-12), apparently Discord returns these weird responses occasionally. Also not our problem.
            if (e is BadRequestException bre && bre.ResponseBody.Contains("<center>nginx</center>")) return false;
            if (e is NotFoundException ne && ne.ResponseBody.Contains("<center>nginx</center>")) return false;
            if (e is UnauthorizedException ue && ue.ResponseBody.Contains("<center>nginx</center>")) return false;

            // Filter out timeout/ratelimit related stuff
            if (e is TooManyRequestsException) return false;
            if (e is RatelimitBucketExhaustedException) return false;
            if (e is TimeoutRejectedException) return false;

            // 5xxs? also not our problem :^)
            if (e is UnknownDiscordRequestException udre && (int)udre.StatusCode >= 500) return false;

            // Webhook server errors are also *not our problem*
            // (this includes rate limit errors, WebhookRateLimited is a subclass)
            if (e is WebhookExecutionErrorOnDiscordsEnd) return false;

            // Socket errors are *not our problem*
            if (e.GetBaseException() is SocketException) return false;

            // Tasks being cancelled for whatver reason are, you guessed it, also not our problem.
            if (e is TaskCanceledException) return false;

            // Sometimes Discord just times everything out.
            if (e is TimeoutException) return false;

            // HTTP/2 streams are complicated and break sometimes.
            if (e is HttpRequestException) return false;

            // Ignore "Database is shutting down" error
            if (e is PostgresException pe && pe.SqlState == "57P03") return false;

            // Ignore thread pool exhaustion errors
            if (e is NpgsqlException npe && npe.Message.Contains("The connection pool has been exhausted")) return false;

            // This may expanded at some point.
            return true;
        }

        public static string GenerateThumbnailForChao(Core.Chao chao)
        {
            var urlRoot = "https://nephanim.com/chao/resources/";
            var shiny = chao.IsShiny ? "shiny_" : "";
            var twoTone = chao.IsTwoTone ? "_twotone" : "";
            var color = chao.PrimaryColor.ToString().ToLower();
            var mixColor = chao.SecondaryColor.HasValue ? $"_{chao.SecondaryColor.Value.ToString().ToLower()}" : "";
            var firstAbilityType = chao.FirstEvolutionType.HasValue ? $"{chao.FirstEvolutionType.Value.ToString().ToLower()}/" : "";
            var secondAbilityType = chao.SecondEvolutionType.HasValue ? $"{chao.SecondEvolutionType.Value.ToString().ToLower()}/" : "";

            // Alignment is determined upon first evolution, so if the chao is still a child, we'll adjust it based on their current leaning
            var alignment = chao.Alignment.ToString().ToLower();
            if (chao.EvolutionState == Core.Chao.EvolutionStates.Child)
            {
                if (chao.AlignmentValue >= 50)
                    alignment = "hero/";
                else if (chao.AlignmentValue <= -50)
                    alignment = "dark/";
                else
                    alignment = "neutral/";
            }
            var child = chao.EvolutionState == Core.Chao.EvolutionStates.Child ? "child/" : "";

            // Examples (separated out for clarity):
            //  https://bytebarcafe.com/chao/resources/ child/ neutral/ shiny_orange_twotone.jpg
            //  https://bytebarcafe.com/chao/resources/ dark/ fly/ run/ white.jpg
            return $"{urlRoot}{child}{alignment}{firstAbilityType}{secondAbilityType}{shiny}{color}{mixColor}{twoTone}.jpg";
        }
    }
}