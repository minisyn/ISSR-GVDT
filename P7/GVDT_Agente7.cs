using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GVDT_Agente7 : ISSR_Agent
{

    public override void Start()
    {
        Debug.LogFormat("{0}: comienza", Myself.Name);
        ISSRHelp.SetupScoutingLocations(this);
    }
    public override IEnumerator Update()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            if (current_state != ISSRState.Scouting)
            {
                ISSRHelp.UpdateVisitedScoutingLocation(this);
            }
            //if currentstate=ISSRState.O
            current_event = ISSREventType.onTickElapsed;
            current_state = AgentStateMachine();
            share();
        }

    }

    /*
     * Códigos definidos por nosotros para emplear en el parámetro int ucode de los
     * métodos acSendMsg() y acSendMsgObj() con el fin de distinguir para que se utiliza
     * el mensaje.
     */
    enum GVDT_MsgCode
    {
        AvailableStone,
        NonAvailableStone,
        LetsGoToGoal,
        ExploredLocation,
        BStoneNonAvaiable
    }
    ISSRState AgentStateMachine()//Función principal de máquina de estados
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual

        switch (current_state)

        {
            case ISSRState.GoingToGripBigStone:

                next_state = SF_GoingToGripBigStone();
                break;

            case ISSRState.WaitingForHelpToMoveBigStone:

                next_state = SF_WaitingForHelpToMoveBigStone();
                break;

            case ISSRState.WaitforNoStonesMovingBigStone:

                next_state = SF_WaitforNoStonesMovingBigStone();
                break;

            case ISSRState.GoingToGoalWithBigStone:

                next_state = SF_GoingToGoalWithBigStone();
                break;

            case ISSRState.SleepingAfterCollisions:

                next_state = SF_SleepingAfterCollisions();
                break;

            case ISSRState.Idle:

                next_state = SF_Idle();
                break;

            case ISSRState.WaitforNoStonesMoving:

                next_state = SF_WaitForNoStonesMoving();
                break;

            case ISSRState.AvoidingObstacle:

                next_state = SF_AvoidingObstacle();
                break;

            case ISSRState.GoingToGripSmallStone:

                next_state = SF_GoingToGripSmallStone();
                break;

            case ISSRState.GoingToGoalWithSmallStone:

                next_state = SF_GoingToGoalWithSmallStone();
                break;

            case ISSRState.Scouting:
                next_state = SF_Scouting();
                break;

            case ISSRState.End:
                break;

            case ISSRState.Error:
                break;

            default:

                Debug.LogFormat("{0}: Estado '{1}' no considerado", Myself.Name, current_state);
                break;

        }
        if (current_state != next_state) //si ha cambiado el estado
        {
            Debug.LogWarningFormat("{0}: Estado '{1}'-->'{2}' por evento '{3}'", Myself.Name, current_state, next_state, current_event);
        }

        return next_state;

    }

    //====================FUNCIONES DE ESTADO (SF)--> "State Function"=======================================================

    ISSRState SF_GoingToGripBigStone()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onTickElapsed:

                next_state = GetBStone(focus_object);
                break;

            case ISSREventType.onDestArrived:

                focus_object.TimeStamp = Time.time;
                BStoneIsAvailable(focus_object, false);
                next_state = ISSRState.Idle;
                break;

            case ISSREventType.onObjectLost:

                focus_object.TimeStamp = Time.time;
                BStoneIsAvailable(focus_object, false);
                next_state = ISSRState.Idle;
                break;

            case ISSREventType.onGripFailure:

                focus_object.TimeStamp = Time.time;
                BStoneIsAvailable(focus_object, false);
                next_state = ISSRState.Idle;
                break;

            case ISSREventType.onCollision:

                if (focus_object.Equals(colliding_object))//Si el objeto con el que ha colisionado es el que tenía como objetivo se solicita volver a coger el objeto
                    next_state = GetBStone(focus_object);
                else
                    next_state = processCollision();
                break;

            case ISSREventType.onManyCollisions:

                if (focus_object.Equals(colliding_object))//Si el objeto con el que ha colisionado es el que tenía como objetivo se solicita volver a coger el objeto
                    next_state = GetBStone(focus_object);
                else
                    next_state = processCollision();
                break;

            case ISSREventType.onGripSuccess:

                if (oiGrippingAgents(focus_object) > 1)
                {
                    focus_object.TimeStamp = Time.time;
                    BStoneIsAvailable(focus_object, false);
                    next_state = ISSRState.WaitforNoStonesMovingBigStone;
                }
                else
                    next_state = ISSRState.WaitingForHelpToMoveBigStone;
                break;

            case ISSREventType.onEnterSensingArea:

                if (focus_object.Equals(object_just_seen)) next_state = GetBStone(focus_object);
                break;

            default:

                if (current_event != ISSREventType.onTickElapsed)
                    Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }

        return next_state;
    }
    ISSRState SF_WaitingForHelpToMoveBigStone()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onUngrip:

                next_state = GetBStone(focus_object);
                break;

            case ISSREventType.onAnotherAgentGripped:

                focus_object.TimeStamp = Time.time;
                BStoneIsAvailable(focus_object, false);
                next_state = ISSRState.WaitforNoStonesMovingBigStone;
                break;

            case ISSREventType.onCollision:

                next_state = GetBStone(focus_object);
                break;

            case ISSREventType.onGObjectCollision:

                next_state = GetBStone(focus_object);
                break;

            default:
                if (current_event != ISSREventType.onTickElapsed)
                    Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;

        }

        return next_state;
    }
    ISSRState SF_WaitforNoStonesMovingBigStone()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onUngrip:

                focus_object.TimeStamp = Time.time;
                BStoneIsAvailable(focus_object, true);
                next_state = GetBStone(focus_object);
                break;

            case ISSREventType.onMsgArrived:

                if ((user_msg_code == (int)GVDT_MsgCode.LetsGoToGoal && msg_obj.Equals(focus_object)) && (iMovingStonesInMyTeam() == 0))
                {
                    acGotoLocation(iMyGoalLocation());
                    next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.GoingToGoalWithBigStone);
                }
                else
                    next_state = ISSRState.WaitforNoStonesMovingBigStone;
                break;

            case ISSREventType.onTickElapsed:

                if (iMovingStonesInMyTeam() == 0)
                {
                    acSendMsgObj(ISSRMsgCode.Assert, (int)GVDT_MsgCode.LetsGoToGoal, focus_object);
                    acGotoLocation(iMyGoalLocation());
                    next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.GoingToGoalWithBigStone);
                }
                break;

            case ISSREventType.onAnotherAgentUngripped:

                focus_object.TimeStamp = Time.time;
                BStoneIsAvailable(focus_object, true);
                next_state = ISSRState.WaitingForHelpToMoveBigStone;
                break;

            default:
                if (current_event != ISSREventType.onTickElapsed)
                    Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;

        }

        return next_state;
    }

    //Asegurarse de que no hay obstaculos en el camino de forma predictiva (Con OnEnterSensingArea)

    ISSRState SF_GoingToGoalWithBigStone()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            
            case ISSREventType.onPushTimeOut:
                if (oiGrippingAgents(GrippedObject) > 1)
                    next_state = ISSRState.WaitforNoStonesMovingBigStone;
                else
                    next_state = ISSRState.WaitingForHelpToMoveBigStone;
                break;

            case ISSREventType.onGObjectScored:

                next_state = ISSRState.Idle;
                break;

            case ISSREventType.onEnterSensingArea:
                Debug.LogFormat("{0}: Empiezo a ver '{1}'", Myself.Name, object_just_seen.Name);


                break;

            case ISSREventType.onCollision:

                next_state = ISSRState.WaitforNoStonesMovingBigStone;
                break;

            case ISSREventType.onGObjectCollision:

                next_state = ISSRState.WaitforNoStonesMovingBigStone;
                break;

            case ISSREventType.onStop:

                next_state = ISSRState.WaitforNoStonesMovingBigStone;
                break;

            case ISSREventType.onAnotherAgentUngripped:

                next_state = ISSRState.WaitingForHelpToMoveBigStone;
                break;

            case ISSREventType.onUngrip:
                if (oiSensable(focus_object))
                {
                    focus_object.TimeStamp = Time.time;
                    BStoneIsAvailable(focus_object, true);
                    next_state = GetBStone(focus_object);
                }
                else
                    next_state = ISSRState.Idle;
                break;

            case ISSREventType.onMsgArrived:
                if ((user_msg_code == (int)GVDT_MsgCode.LetsGoToGoal && msg_obj.Equals(focus_object)) && (iMovingStonesInMyTeam() == 0))
                {
                    acGotoLocation(iMyGoalLocation());
                    next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.GoingToGoalWithBigStone);
                }
                else
                    next_state = ISSRState.WaitforNoStonesMovingBigStone;
                break;
            default:
                if (current_event != ISSREventType.onTickElapsed)
                    Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }

        return next_state;
    }
    ISSRState SF_SleepingAfterCollisions()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {
            case ISSREventType.onUngrip:

                focus_object.TimeStamp = Time.time;
                SStoneIsAvailable(focus_object, true);
                next_state = GetSStone(focus_object);
                break;

            case ISSREventType.onTimerOut:

                next_state = resumeAfterCollision();
                break;

            case ISSREventType.onDestArrived:

                next_state = resumeAfterCollision();
                break;

            case ISSREventType.onCollision:

                next_state = ISSRState.SleepingAfterCollisions;
                break;

            default:
                if (current_event != ISSREventType.onTickElapsed)
                    Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }

        return next_state;

    }
    ISSRState SF_Idle()
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual
        int remain;
        focus_location = ISSRHelp.GetCloserToMeLocationInList(this, Valid_Locations, out remain);
        switch (current_event)
        {
            case ISSREventType.onTickElapsed:

                focus_object = ISSRHelp.Get_next_available_stone_closer_to_me(this);//Elige la piedra más cercana al agente, ya sea pequeña o grande
                if (focus_object != null)//si hay alguna piedra  
                {
                    if (focus_object.type == ISSR_Type.SmallStone) next_state = GetSStone(focus_object);
                    else if (focus_object.type == ISSR_Type.BigStone) next_state = GetBStone(focus_object);
                }
                else if (remain > 0)
                {
                    acGotoLocation(focus_location);
                    next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.Scouting);
                }
                else//Si focus_object es null significa que no hay más piedras disponibles ni areas -- ¿Nos ponemos a molestar al equipo contrario?
                {
                    next_state = ISSRState.End;
                    Debug.LogFormat("{0}: No conozco más piedras", Myself.Name);

                }
                break;
            case ISSREventType.onEnterSensingArea:
                //onEnterSensingArea(object_just_seen); -- ver qué entra en nuestra area (Si es agente, y si agarra piedra y si es de nuestro equipo avoid colision)
                break;
            case ISSREventType.onCollision:

                next_state = processCollision();
                break;

            case ISSREventType.onManyCollisions:

                next_state = processCollision();
                break;

            default:

                Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;

        }

        return next_state;

    }
    ISSRState SF_AvoidingObstacle()
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual

        switch (current_event)
        {
            case ISSREventType.onManyCollisions:

                acSetTimer(UnityEngine.Random.value * 2f);//lanza un temporizador aleatorio entre 0 y 2 segundos
                next_state = ISSRState.SleepingAfterCollisions;
                break;

            case ISSREventType.onCollision:

                next_state = processCollision();
                break;

            case ISSREventType.onGObjectCollision:

                next_state = processCollision();
                break;

            case ISSREventType.onGObjectScored:

                Debug.LogFormat("{0}: Se ha introducido la piedra '{1}' mientras se esquivaba un obstáculo", Myself.Name, focus_object.Name);
                next_state = ISSRState.Idle;
                break;

            case ISSREventType.onDestArrived:

                next_state = resumeAfterCollision();
                break;

            case ISSREventType.onEnterSensingArea:
                Debug.LogFormat("{0}: Empiezo a ver '{1}'", Myself.Name, object_just_seen.Name);

                break;

            default:
                if (current_event != ISSREventType.onTickElapsed)
                    Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;

        }

        return next_state;

    }
    ISSRState SF_WaitForNoStonesMoving()
    {
        ISSRState next_state = current_state;

        switch (current_event)
        {

            case ISSREventType.onUngrip:

                focus_object.TimeStamp = Time.time;//marcamos el tiempo en el que se informa de la piedra
                SStoneIsAvailable(focus_object, true);
                next_state = GetSStone(focus_object);
                break;

            case ISSREventType.onTickElapsed:

                if (iMovingStonesInMyTeam() == 0)//si no hay piedras en movimiento pide moverse a la meta
                {
                    acGotoLocation(iMyGoalLocation());
                    next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.GoingToGoalWithSmallStone);
                    if (next_state == ISSRState.GoingToGoalWithSmallStone)
                    {
                        Debug.LogFormat("{0}: Piedra '{1}' agarrada con éxito", Myself.Name, focus_object.Name);
                    }

                }
                break;
            case ISSREventType.onEnterSensingArea:
                //Evento no considerado en este estado
                Debug.LogFormat("{0}: Empiezo a ver '{1}'", Myself.Name, object_just_seen.Name);
                break;
            default:
                if (current_event != ISSREventType.onTickElapsed)
                    Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }

        return next_state;

    }
    ISSRState SF_GoingToGripSmallStone()//SF--> "State Function"
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual

        switch (current_event)
        {
            case ISSREventType.onTickElapsed:

                next_state = GetSStone(focus_object);//INTENTO COGER LA PIEDRA CADA CIERTO TIEMPO PARA SABER SI SIGUE ESTANDO SIPONIBLE
                break;
            case ISSREventType.onObjectLost:

                focus_object.TimeStamp = Time.time;//marcamos el tiempo en el que se informa de la piedra
                SStoneIsAvailable(focus_object, false);
                next_state = ISSRState.Idle;

                break;
            case ISSREventType.onGripFailure:

                focus_object.TimeStamp = Time.time;//marcamos el tiempo en el que se informa de la piedra
                SStoneIsAvailable(focus_object, false);
                next_state = ISSRState.Idle;

                break;
            case ISSREventType.onDestArrived:

                SStoneIsAvailable(focus_object, false);
                next_state = ISSRState.Idle;

                break;
            case ISSREventType.onGripSuccess:

                focus_object.TimeStamp = Time.time;//marcamos el tiempo en el que se informa de la piedra
                SStoneIsAvailable(focus_object, false);
                next_state = ISSRState.WaitforNoStonesMoving;

                break;

            case ISSREventType.onEnterSensingArea:

                if (object_just_seen.Equals(focus_object)) next_state = GetSStone(focus_object);
                //else{

            //  }
                break;

            case ISSREventType.onCollision:

                if (focus_object.Equals(colliding_object))//Si el objeto con el que ha colisionado es el que tenía como objetivo se solicita volver a coger el objeto
                    next_state = GetSStone(focus_object);
                else
                    next_state = processCollision();
                break;

            case ISSREventType.onManyCollisions:

                if (focus_object.Equals(colliding_object))//Si el objeto con el que ha colisionado es el que tenía como objetivo se solicita volver a coger el objeto
                    next_state = GetSStone(focus_object);
                else
                    next_state = processCollision();
                break;

            default:
                if (current_event != ISSREventType.onTickElapsed)
                    Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;

        }

        return next_state;

    }
    ISSRState SF_GoingToGoalWithSmallStone()
    {
        ISSRState next_state = current_state;//Estado de salida , en principio igual que el estado actual

        switch (current_event)
        {
            case ISSREventType.onGObjectScored:

                next_state = ISSRState.Idle;//Pasa a estado inactivo
                Debug.LogFormat("{0}: Piedra '{1}' introducida con éxito", Myself.Name, focus_object.Name);
                break;

            case ISSREventType.onCollision:

                next_state = processCollision();
                break;

            case ISSREventType.onGObjectCollision:

                next_state = processCollision();
                break;

            case ISSREventType.onEnterSensingArea:
                //Evento no considerado en este estado
                Debug.LogFormat("{0}: Empiezo a ver '{1}'", Myself.Name, object_just_seen.Name);
                break;

            default:

                if (ISSREventType.onTickElapsed != current_event)
                    Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;
        }

        return next_state;

    }
    ISSRState SF_Scouting()
    {
        
        ISSRState next_state = current_state;
        int remain;


        switch (current_event)
        {
            case ISSREventType.onTickElapsed:

                focus_object = ISSRHelp.Get_next_available_stone_closer_to_me(this);//Elige la piedra más cercana al agente, ya sea pequeña o grande
                if (focus_object != null)//si hay alguna piedra  -- se puede comunicar al resto e ir a por ella
                {
                    if (focus_object.type == ISSR_Type.SmallStone) next_state = GetSStone(focus_object);
                    else if(focus_object.type == ISSR_Type.BigStone) next_state = GetBStone(focus_object);
                }
                else if (ISSRHelp.UpdateVisitedScoutingLocation(this))
                {
                    focus_location = ISSRHelp.GetCloserToMeLocationInList(this, Valid_Locations, out remain);
                    if (remain > 0)
                    {
                        acGotoLocation(focus_location);
                        next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.Scouting);
                    }
                    else
                        next_state = ISSRState.Idle;
                }
                else
                {
                    acGotoLocation(focus_location);
                    next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.Scouting);
                }

                break;

            case ISSREventType.onCollision:

                next_state = processCollision();
                break;

            case ISSREventType.onManyCollisions:

                next_state = processCollision();
                break;

            default:
                if (current_event != ISSREventType.onTickElapsed)
                    Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                break;

        }

        return next_state;

    }

    //=====================FUNCIONES DE EVENTOS==================================================================================

    public override void onEnterSensingArea(ISSR_Object obj)
    {
        object_just_seen = obj;//Anotams objeto que vemos

        if (obj.type == ISSR_Type.SmallStone)
        {
            if (oiGrippingAgents(obj) > 0) SStoneIsAvailable(obj, false);
            else SStoneIsAvailable(obj, true);
        }
        else if (obj.type == ISSR_Type.BigStone)
        {
            if (oiGrippingAgents(obj) < 2) BStoneIsAvailable(obj, true);
            else BStoneIsAvailable(obj, false);
        }
        current_state = AgentStateMachine();

        //if es agente -- calcular mi distancia con él y trayectoria avoiding object si me cruzo 
    }
    public override void onGripSuccess(ISSR_Object obj_gripped)
    {
        Debug.LogFormat("{0}: piedra '{1}' agarrada ", Myself.Name, obj_gripped.Name);
        current_state = AgentStateMachine();

    }
    public override void onGObjectScored(ISSR_Object stone_that_scored)
    {
        Debug.LogFormat("{0}: piedra '{1}' metida en meta", Myself.Name, stone_that_scored.Name);
        current_state = AgentStateMachine();
    }
    public override void onCollision(ISSR_Object obj_that_collided_with_me)
    {
        Debug.LogFormat("{0}: He colisionado con '{1}'", Myself.Name, obj_that_collided_with_me.Name);
        colliding_object = obj_that_collided_with_me;
        current_state = AgentStateMachine();
    }
    public override void onGObjectCollision(ISSR_Object obj_that_collided_with_gripped_obj)
    {
        Debug.LogFormat("{0}: El objeto que tengo agarrado ha colisionado con '{1}'", Myself.Name, obj_that_collided_with_gripped_obj.Name);
        colliding_object = obj_that_collided_with_gripped_obj;
        current_state = AgentStateMachine();
    }
    public override void onDestArrived()
    {
        current_state = AgentStateMachine();
    }
    public override void onGripFailure(ISSR_Object obj_I_wanted_to_grip)
    {
        current_state = AgentStateMachine();
    }
    public override void onObjectLost(ISSR_Object obj_i_was_looking_for)
    {
        current_state = AgentStateMachine();
    }
    public override void onUngrip(ISSR_Object ungripped_object)
    {
        current_state = AgentStateMachine();
    }
    public override void onAnotherAgentGripped(ISSR_Object agent)
    {
        current_state = AgentStateMachine();
    }
    public override void onAnotherAgentUngripped(ISSR_Object agent)
    {
        current_state = AgentStateMachine();
    }
    public override void onPushTimeOut(ISSR_Object gripped_big_stone)
    {
        current_state = AgentStateMachine();
    }
    public override void onTimerOut(float delay)
    {
        current_state = AgentStateMachine();
    }
    public override void onManyCollisions()
    {
        current_state = AgentStateMachine();
    }
    public override void onMsgArrived(ISSR_Message msg)
    {
        processMessage(msg);
    }

    //=====================FUNCIONES AUXILIARES====================================================================================

    /*
    * Método mediante el cual el agente procesa los mensajes para actualizar 
    * las listas de las piedras conforme a la información de las piedras de las 
    * que se informa en dichos mensajes.
    */
    private void processMessage(ISSR_Message msg)
    {


        switch (msg.usercode)
        {
            case (int)GVDT_MsgCode.LetsGoToGoal:

                if (msg_obj.Equals(focus_object)) current_state = AgentStateMachine();

                break;

            case (int)GVDT_MsgCode.AvailableStone:

                if (msg_obj.type == ISSR_Type.SmallStone) SStoneIsAvailable(msg.Obj, true);
                else BStoneIsAvailable(msg.Obj, true);

                break;

            case (int)GVDT_MsgCode.NonAvailableStone:

                if (msg_obj.type == ISSR_Type.SmallStone) SStoneIsAvailable(msg.Obj, false);
                else BStoneIsAvailable(msg.Obj, false);

                break;

            case (int)GVDT_MsgCode.ExploredLocation:

                if (Valid_Locations.Contains(msg_location))
                {
                    Valid_Locations.Remove(msg_location);
                    Invalid_Locations.Add(msg_location);
                }

                break;

            case (int)GVDT_MsgCode.BStoneNonAvaiable:
                BStoneIsAvailable(msg.Obj, false);
                break;
        }
    }
    /*
    * Método mediante el cual el agente trasnmite a los demás la información que 
    * contiene en sus listas de piedras (tanto las que están disponibles como las
    * que no lo están.
    */
    private void share()
    {
        foreach (ISSR_Object piedraDisponible in Valid_Small_Stones)
            acSendMsgObj(ISSRMsgCode.Assert, (int)GVDT_MsgCode.AvailableStone, piedraDisponible);


        foreach (ISSR_Object piedraNoDisponible in Invalid_Small_Stones)
            acSendMsgObj(ISSRMsgCode.Assert, (int)GVDT_MsgCode.NonAvailableStone, piedraNoDisponible);


        foreach (ISSR_Object piedraDisponible in Valid_Big_Stones)
            acSendMsgObj(ISSRMsgCode.Assert, (int)GVDT_MsgCode.AvailableStone, piedraDisponible);


        foreach (ISSR_Object piedraNoDisponible in Invalid_Big_Stones)
            acSendMsgObj(ISSRMsgCode.Assert, (int)GVDT_MsgCode.NonAvailableStone, piedraNoDisponible);


        foreach (Vector3 ubicacionexplorada in Invalid_Locations)
            acSendMsgObj(ISSRMsgCode.Assert, (int)GVDT_MsgCode.ExploredLocation, Myself, ubicacionexplorada, 0, 0);

    }
    private ISSRState processCollision()  // Procesar colisi�n 
    {
        ISSRState next_state = current_state;

        switch (current_state)
        {
            case ISSRState.GoingToGripBigStone:

                last_state = current_state;//Guarda el estado actual en el último estado para así poder recuperarlo más tarde y poder completar lo que se estaba llevando a cabo antes de la colisión
                next_state = acGotoSafeLocation();
                break;

            case ISSRState.GoingToGripSmallStone:

                last_state = current_state;//Guarda el estado actual en el último estado para así poder recuperarlo más tarde y poder completar lo que se estaba llevando a cabo antes de la colisión
                next_state = acGotoSafeLocation();
                break;

            case ISSRState.GoingToGoalWithSmallStone:

                last_state = current_state;//Guarda el estado actual en el último estado para así poder recuperarlo más tarde y poder completar lo que se estaba llevando a cabo antes de la colisió
                if (iMovingStonesInMyTeam() == 0)
                    next_state = acGotoSafeLocation();
                else
                    next_state = ISSRState.WaitforNoStonesMoving;
                break;

            case ISSRState.GoingToGoalWithBigStone:

                last_state = current_state;//Guarda el estado actual en el último estado para así poder recuperarlo más tarde y poder completar lo que se estaba llevando a cabo antes de la colisió
                if (iMovingStonesInMyTeam() == 0)
                    next_state = ISSRState.WaitingForHelpToMoveBigStone;
                else
                    next_state = ISSRState.WaitforNoStonesMovingBigStone;
                break;

            case ISSRState.AvoidingObstacle:

                last_state = current_state;
                next_state = acGotoSafeLocation();
                break;

            case ISSRState.Scouting:
                
                last_state = current_state;
                next_state = acGotoSafeLocation();
                //HINT: Podría hacerse comprobación y si es un muñeco, que el nuestro continue su camino
                break;

            case ISSRState.Idle:

                last_state = current_state;
                next_state = acGotoSafeLocation();
                break;

            default:

                if (current_event != ISSREventType.onTickElapsed)
                    Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                next_state = ISSRState.Error;//Da error para avisarnos en el futuro de que hemos entrado a un estado aún no considerado
                break;
        }

        return next_state;
    }
    private ISSRState resumeAfterCollision() // Permite continuar con lo que se estaba haciendo en el momento de la colisión.
    {
        ISSRState next_state = current_state;

        int remain;

        switch (last_state)  // Según estado anterior 
        {
            case ISSRState.GoingToGripBigStone:

                next_state = GetBStone(focus_object);  // Volver a pedir coger piedra o ir a su lugar
                break;

            case ISSRState.GoingToGripSmallStone:

                next_state = GetSStone(focus_object);  // Volver a pedir coger piedra o ir a su lugar
                break;

            case ISSRState.GoingToGoalWithSmallStone:

                if (iMovingStonesInMyTeam() == 0)//si no hay piedras en movimiento
                {
                    acGotoLocation(iMyGoalLocation());  // volver a pedir ir a la meta
                    next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.GoingToGoalWithSmallStone);

                } else next_state = ISSRState.WaitforNoStonesMoving;
                
                
                break;
            
            case ISSRState.GoingToGoalWithBigStone:

                Debug.LogError("Estado no considerado en resumeAfterCollision");
                if(iMovingStonesInMyTeam() == 0){
                        acGotoLocation(iMyGoalLocation());  // volver a pedir ir a la meta
                    next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.GoingToGoalWithBigStone);
                }else next_state = ISSRState.WaitforNoStonesMovingBigStone;
                break;

            case ISSRState.Scouting:
                //Comprobamos si se ha visitado la localización, en caso de ser así se actualiza a la posición más cercana al agente
                if (ISSRHelp.UpdateVisitedScoutingLocation(this))
                {
                    focus_location = ISSRHelp.GetCloserToMeLocationInList(this, Valid_Locations, out remain);
                    if (remain > 0)
                    {
                        acGotoLocation(focus_location);
                        next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.Scouting);
                    }
                    else
                        next_state = ISSRState.Idle;
                }
                else
                {
                    acGotoLocation(focus_location);
                   next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.Scouting);
                }

                break;
            
            case ISSRState.Idle:

                next_state = ISSRState.Idle;
                break;

            default:

                if (current_event != ISSREventType.onTickElapsed)
                    Debug.LogWarningFormat("{0}: Evento '{1}' no considerado en estado '{2}'", Myself.Name, current_event, current_state);
                next_state = ISSRState.Error;//Da error para avisarnos de que hemos vuelto a un estado que aún no se ha considerado en esta función
                break;
        }
        return next_state;
    }
    /*
    * Método para llevar contabilidad de las piedras grandes disponibles.
    * Sirve para mantener actualizada la lista de piedras disponibles del agente,admitir
    * o descartar piedras grandes
    */
    private void BStoneIsAvailable(ISSR_Object stone, bool available)
    {
        ISSRHelp.UpdateStoneLists(stone, available, Valid_Big_Stones, Invalid_Big_Stones);
    }
    /*
    * Método para intentar conseguir una piedra grande. Método a llamar cuando el agente
    * vaya a considerar procesar una nueva piedra: tendrá en cuenta tanto si es visible
    * como si no está visible y solo lo intentará si está disponible o al menos eso es
    * lo que el agente "cree"
    */
    private ISSRState GetBStone(ISSR_Object stone)
    {
        ISSRState next_state = current_state;

        if (oiSensable(stone))//si está visible
        {
            if (oiGrippingAgents(stone) > 1)//si hay más de un agente agarrando la piedra
            {
                
                next_state = descartarPiedraGrandePararYPasarAEstadoIDLE(stone);
            }
            else
            {
                //enviar mensaje -- voy a por la piedra X
                acSendMsgObj(ISSRMsgCode.Assert, (int)GVDT_MsgCode.BStoneNonAvaiable, focus_object);
                acGripObject(stone);
                next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.GoingToGripBigStone);

            }
        }
        else//si no está visible
        {
            if (ISSRHelp.Distance_from_object_to_me(this, focus_object) < iSensingRange())
            {
                next_state = descartarPiedraGrandePararYPasarAEstadoIDLE(stone);
            }
            else
            {
                acGotoLocation(stone.LastLocation);//pedimos ir a la última localización de la piedra
                next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.GoingToGripBigStone);
            }

        }

        return next_state;

    }
    /*
     * Método para llevar contabilidad de las piedras pequeñas disponibles.
     * Sirve para mantener actualizada la lista de piedras disponibles del agente,admitir
     * o descartar piedras pequeñas
     */
    private void SStoneIsAvailable(ISSR_Object stone, bool available)
    {
        ISSRHelp.UpdateStoneLists(stone, available, Valid_Small_Stones, Invalid_Small_Stones);
    }
    /*
     * Método para intentar conseguir una piedra pequeña.Método a llamar cuando el agente
     * vaya a considerar procesar una nueva piedra: tendrá en cuenta tanto si es visible
     * como si no está visible y solo lo intentará si está disponible o al menos eso es
     * lo que el agente "cree"
     */
    private ISSRState GetSStone(ISSR_Object stone)
    {
        ISSRState next_state = current_state;

        if (oiSensable(stone))//si está visible
        {
            if (oiGrippingAgents(stone) > 0)//si hay agentes agarradno la piedra
            {

                next_state = descartarPiedraPequeñaPararYPasarAEstadoIDLE(stone);
            }
            else
            {
                acGripObject(stone);
                next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.GoingToGripSmallStone);

            }
        }
        else//si no está visible
        {
            if (ISSRHelp.Distance_from_object_to_me(this, focus_object) < iSensingRange())
            {
                next_state = descartarPiedraPequeñaPararYPasarAEstadoIDLE(stone);
            }
            else
            {
                acGotoLocation(stone.LastLocation);//pedimos ir a la última localización de la piedra
                next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.GoingToGripSmallStone);
            }

        }

        return next_state;

    }
    private ISSRState descartarPiedraPequeñaPararYPasarAEstadoIDLE(ISSR_Object stone)
    {
        ISSRState next_state = current_state;
        stone.TimeStamp = Time.time;//marcamos el tiempo en el que se informa de la piedra
        SStoneIsAvailable(stone, false);//descartamos la piedra de la lista de piedras disponibles
        acStop();//pedimos parar al agente
        next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.Idle);

        return next_state;
    }
    ///!!!!!!!!!!!!UNIFICAR EL CÓDIGO DE ESTAS DOS FUNCIONES
    private ISSRState descartarPiedraGrandePararYPasarAEstadoIDLE(ISSR_Object stone)
    {
        ISSRState next_state = current_state;
        stone.TimeStamp = Time.time;//marcamos el tiempo en el que se informa de la piedra
        BStoneIsAvailable(stone, false);//descartamos la piedra de la lista de piedras disponibles
        acStop();//pedimos parar al agente
        next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.Idle);

        return next_state;
    }
    private ISSRState comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState estadoAlqueSeQuierePasar)
    {

        if (acCheckError())//si hay error en la acción
        {
            return ISSRState.Error;//señala error
        }
        else
        {
            return estadoAlqueSeQuierePasar;
        }

    }
    private ISSRState acGotoSafeLocation()
    {

        ISSRState next_state;
        if ((last_state == ISSRState.GoingToGoalWithSmallStone) && (iMovingStonesInMyTeam() != 0))
            next_state = ISSRState.WaitforNoStonesMoving;
        else if (last_state == ISSRState.GoingToGoalWithBigStone && (iMovingStonesInMyTeam() != 0))
            next_state = ISSRState.WaitforNoStonesMovingBigStone;
        else{
                acGotoLocation(ISSRHelp.CalculateSafeLocation(this, colliding_object));
                next_state = comprobarErrorEnAccionYPasarASiguienteEstado(ISSRState.AvoidingObstacle);
        }
        return next_state;
    }

    // private void evitarChoque(ISSR_){
    //     //Agentes dentro de mi rango sensible
    //     if(oiSensable(obj)){

    //     }
    // }
}
