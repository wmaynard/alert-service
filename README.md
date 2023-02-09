# Alert Service

## Introduction

Good logging is always a great tool for diagnosing issues.  Clear logs reduce the amount of time it takes to find an issue - sometimes making it near-instantaneous to find problematic code - but even the best logs need to be inspected to be useful.  At Rumble, we have a comprehensive weekly review and a daily email summary to help surface worrying trends as they happen - but sometimes even this falls short of what's needed.

Sometimes, we need to know as _soon as possible_ that something is wrong.  Take, for example, that the CDN url from Dynamic Config is failing.  There are a many reasons why this could happen, and to name a few:

1. The Config value was changed, and there was a typo in the URL.
2. The Config value was imported from another environment, and the dev CDN won't work for prod.
3. Player-service is down.
4. AWS is down.

If the CDN url fails, no one can access the game.  And if we aren't testing the login 24/7, we may not realize something is wrong for several hours, at least.  In fact, we've even had one situation in the Tower Heroes alpha where the prod environment was inaccessible for 9 hours.

Wouldn't it be nice to have near-instant notifications?

## Glossary

| Term         | Definition |
|:-------------|:-----------|
| Alert        |            |
| Escalation   |            |
| Impact       |            |
| Notification |            |
| Owner        |            |
| Trigger      |            |

### Statuses


| Status        | Definition                                                                                                   |
|:--------------|:-------------------------------------------------------------------------------------------------------------|
| Pending       | The Alert is new, and will not send until its Trigger is met.                                                |
| Sent          | The Alert has been sent, but not yet acted upon.                                                             |
| Acknowledged  | Someone saw the Alert and has clicked a link indicating they're looking into it.                             |
| Escalated     | The Alert has progressed to at least one higher stage of notification.                                       |
| PendingResend | An open Alert has been hit again, and it's been long enough that the Alert will shortly send a reminder out. |
| Resolved      | Whatever problem has occurred, someone has fixed the issue, and is closing the alert.                        |
| TriggerNotMet | The Alert was automatically closed because the Trigger condition had not been reached.                       |
| Canceled      | The Alert was deemed a false alarm.  This indicates no action is required.                                   |


## Enter Alerts & Notifications

Modeled after PagerDuty, Alerts aim to solve similar use cases - but allow us to customize behavior to our exact needs, and give us built-in control in the code.  Most importantly, the end goal is to have something that's a cross between PagerDuty and a Loggly entry; detailed logs, stack traces, and diagnostic information so we can very quickly react to issues in real-time.  Of course, the alert-service is intended to be _supplemental_, not a replacement for PagerDuty.

For the purposes of this documentation, an `Alert` is a form of digital communication that indicates that **something is wrong**.  Most Alerts issued will mean there is direct player impact, though this is not necessarily the case.  An Alert will have an escalation policy - that is, if nothing is done about it, the alert will get noisier, calling more attention to itself.

A `Notification` is a subtype of an `Alert`, but lacks an escalation policy and does not need to be acted upon.  For the time being, this is just a planned feature.

## Triggers

Alerts require a `Trigger`.  This is a set of conditions defined by the implementing developer that determine when an Alert should actually fire.  Alerts are first created with a **Pending** status.  While in this state, they will not fire unless their Trigger is met.

A Trigger consists of two numbers:

* `countRequired`: This is the number of times an Alert must be registered within the `timeframe` before it will actually send.
* `timeframe`: The amount of time, in seconds, that an Alert should remain in Pending status.

Once an Alert has reached its Trigger, the alert-service will send the Alert out as an email and as a Slack message.  If the Trigger condition is not met, the Alert will automatically be resolved.

Some Trigger examples:

* Token generation in player-service fails.  It could be a fluke; maybe token-service is updating or rebooting, so we want to give it some error tolerance.  We allow up to 10 errors in 300 seconds.
* Leaderboards rollover has failed.  Rollovers retry up to 9 extra times, but this may only happen once in a blue moon.  We want to notify immediately, so we set our trigger to 1 error; the timeframe doesn't matter.

Keep in mind that, because it's alert-service tracking these, the Trigger's `countRequired` **does not depend** on the particular instance of your code, but rather _all_ instances of it.  So for that first example, if we have 4 containers for player-service each with 3 Alert requests fired off, an alert will be sent.

## Other Alert Anatomy

| Term             | Definition                                                                                                                 |
|:-----------------|:---------------------------------------------------------------------------------------------------------------------------|
| Owner            | An integer corresponding to the `Owner` enum.  This is used to pull an owner's email address and DM or ping them on Slack. |
| Title            | A short, descriptive string.  Used as the subject line in emails.                                                          |
| Impact           | An integer corresponding to the `Impact` enum in platform-common.                                                          |
| Data             | Any JSON you wish to include in your alert as relevant data.                                                               |

## Creating an Alert by crafting a web request

While the endpoints are accessible to anyone who needs them, this actually isn't the preferred way of sending alerts.  Especially since they rely on enums found in platform-common, it's much safer to use the ApiService.  However, there may be situations - such as a Jenkins job - where we want to use Alerts.

For those cases, send an admin token along with the following request:

```
POST /alert
{
    "alert": {
        "owner": 9,
        "title": "Test alert",
        "message": "Foo",
        "impact": 100,
        "data": { },
        "trigger": {
            "countRequired": 3,
            "timeframe": 60
        }
    }

}
```

## Creating an Alert with with platform-common

With platform-common-1.3.9 or later, you can create an Alert easily with the ApiService:

```
_apiService.Alert(
    title: "Hello, World!",
    message: "This is an example alert from the ApiService!",
    countRequired: 5,
    timeframe: 300,
    data: new RumbleJson
    {
        { "foo", 12345 }
    } 
);
```

Optional fields include `type`, `impact`, and `data`.

## Managing where and when Alerts are sent

Dynamic Config manages the email addresses and channels for alert destinations.  This allows us, if we want to, to create separate channels or groups by environment.

We _probably_ don't want anything noisy in dev, so we can send the emails to a rarely checked email address, or use `+dev` in the email, then each person on the list can automatically filter it.

Similarly, for Slack, we can have separate channels.  Dev might use `#slack-app-sandbox`, whereas prod should be far more visible, such as `#tower-live`.

Dynamic Config also controls `secondsBeforeEscalation`; this 

## Responding to Alerts

When you receive an alert, there will be some links included in them: Acknowledge, Resolve, and Cancel.  Acknowledge will give you extra time before the Alert escalates - at the time of writing, this gives you double the amount of time before escalation.  Resolve will let you close out the alert - this indicates you've fixed whatever was wrong and the Alert is no longer necessary.  Cancel indicates a false alarm, or something that should otherwise be a rare or one-off case.

An Alert can be acknowledged an unlimited number of times, each acknowledgement resetting the timer for escalation.  This is useful for those situations where you need more time to investigate an alert but want to prevent it from escalating further.